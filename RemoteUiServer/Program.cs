// <Project Sdk="Microsoft.NET.Sdk">
//   <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup>
// </Project>

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;
using Hexa.NET.GLFW;
using Hexa.NET.OpenGL;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImPlot;
using Hexa.NET.ImNodes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;
using ELImGui.Utils;
using ELImGui.Window;
using ELImGui;
using System.Threading;

var server = new OffscreenImGuiServer(Constants.WIDTH, Constants.HEIGHT)
{
    RenderDelegate = () =>
    {
        // 메인 메뉴 + 도킹
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("View"))
            {
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
        ImGui.DockSpaceOverViewport(null, ImGuiDockNodeFlags.PassthruCentralNode, null);
        ImGui.PopStyleColor();

        //ImGui.ShowDemoWindow();
    }
};

using var cts = new CancellationTokenSource();

var renderThread = new Thread(() =>
{
    try
    {
        server.StartRenderLoopAsync(cts.Token, Constants.FPS);
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
        Console.WriteLine($"[render] {ex}");
    }
});
renderThread.Start();

// 아주 얇은 HTTP 서버 시작
var http = new TinyHttp(server);
_ = http.StartAsync(IPAddress.Any, Constants.PORT, cts.Token);

Console.WriteLine($"Listening on http://localhost:{Constants.PORT}");
Console.WriteLine("Press ENTER to quit...");
Console.ReadLine();
cts.Cancel();

server.Dispose();
await http.StopAsync();

static class Constants
{
    public const int WIDTH = 1280;
    public const int HEIGHT = 720;
    public const int FPS = 30;
    public const int PORT = 8080; // 필요 시 변경
}

// ---------------------------------------------------------------------
// 얇은 HTTP 서버 (TcpListener로 / 와 /mjpeg만 처리)
// ---------------------------------------------------------------------
sealed class TinyHttp
{
    private TcpListener _listener;
    private readonly OffscreenImGuiServer _imgServer;
    private readonly List<TcpClient> _clients = new();
    private readonly object _gate = new();
    private CancellationTokenSource? _acceptLoopCts;

    public TinyHttp(OffscreenImGuiServer imgServer)
    {
        _listener = new TcpListener(IPAddress.Any, 0);
        _imgServer = imgServer;
    }

    public Task StartAsync(IPAddress ip, int port, CancellationToken token)
    {
        _listener.Server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.LocalEndpoint.GetType(); // no-op
        _listener = new TcpListener(ip, port);
        _listener.Start(128);

        _acceptLoopCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var ct = _acceptLoopCts.Token;
        return Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(ct);
                    client.NoDelay = true;
                    lock (_gate) _clients.Add(client);
                    _ = HandleClientAsync(client, ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Console.WriteLine($"[accept] {ex.Message}"); }
            }
        }, ct);
    }

    public async Task StopAsync()
    {
        try { _acceptLoopCts?.Cancel(); } catch { }
        try { _listener.Stop(); } catch { }

        List<TcpClient> copy;
        lock (_gate) { copy = _clients.ToList(); _clients.Clear(); }
        foreach (var c in copy)
        {
            try { c.Close(); c.Dispose(); } catch { }
        }
        await Task.CompletedTask;
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var _ = client;
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);

        string? requestLine = null;
        try { requestLine = await reader.ReadLineAsync(ct); } catch { return; }
        if (string.IsNullOrEmpty(requestLine)) return;

        var parts = requestLine.Split(' ');
        if (parts.Length < 2) return;
        var method = parts[0];
        var path = parts[1];

        // --- 헤더 파싱 (Content-Length/CORS 대비) ---
        int contentLength = 0;
        //bool isJson = false;
        while (true)
        {
            string? line;
            try { line = await reader.ReadLineAsync(ct); }
            catch { return; }
            if (line == null || line.Length == 0) break;

            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                var h = line.Substring(0, idx).Trim();
                var v = line[(idx + 1)..].Trim();
                if (h.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(v, out contentLength);
                //if (h.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) &&
                //    v.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                //    isJson = true;
            }
        }

        // 프리플라이트(옵션): CORS Preflight 허용
        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await WriteSimple(stream,
                "HTTP/1.1 204 No Content\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: POST, GET, OPTIONS\r\n" +
                "Access-Control-Allow-Headers: Content-Type\r\n" +
                "Content-Length: 0\r\n\r\n");
            return;
        }

        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            await WriteSimple(stream, "HTTP/1.1 405 Method Not Allowed\r\n\r\n");
            return;
        }

        if (path == "/")
        {
            var html = """
<!doctype html>
<meta charset="utf-8"/>
<title>Offscreen ImGui MJPEG</title>
<style>
  html,body{margin:0;background:#111;color:#ddd;font:14px/1.4 system-ui,Segoe UI,Apple SD Gothic Neo}
  .wrap{display:flex;flex-direction:column;align-items:center;gap:12px;padding:16px}
  img{max-width:100%;height:auto;background:#000}
</style>
<div class="wrap">
  <h3>ImGui Offscreen → MJPEG (with input)</h3>
  <div id="frameWrap" style="position:relative;display:inline-block;">
    <img id="view" src="/mjpeg" alt="stream">
    <div id="overlay" style="position:absolute;left:0;top:0;right:0;bottom:0;"></div>
  </div>
  <p>마우스로 영역 위에서 이동/클릭/휠을 하면 서버로 전달됩니다.</p>
</div>
<script>
(function(){
  const W = %WIDTH%;
  const H = %HEIGHT%;
  const img = document.getElementById('view');
  const overlay = document.getElementById('overlay');

  function send(obj){
    fetch('/input', {
      method:'POST',
      headers:{'Content-Type':'application/json'},
      body: JSON.stringify(obj),
      keepalive: true,
    }).catch(()=>{});
  }

  function getScaleAndOffset(){
    const r = img.getBoundingClientRect();
    // 실제 표시 크기 대비 원본 크기 스케일
    const scaleX = W / r.width;
    const scaleY = H / r.height;
    return {r, scaleX, scaleY};
  }

  let lastSent = 0;
  overlay.addEventListener('pointermove', e=>{
    const now = performance.now();
    if (now - lastSent < (1000/%FPS%)) return; // FPS 정도로 스로틀
    lastSent = now;

    const {r, scaleX, scaleY} = getScaleAndOffset();
    const x = (e.clientX - r.left) * scaleX;
    const y = (e.clientY - r.top)  * scaleY;

    send({ type:'move', x, y });
  });

  overlay.addEventListener('pointerdown', e=>{
    const btn = e.button; // 0,1,2
    send({ type:'down', button: btn });
  });
  overlay.addEventListener('pointerup', e=>{
    const btn = e.button;
    send({ type:'up', button: btn });
  });

  overlay.addEventListener('wheel', e=>{
    // 브라우저는 보통 dy>0 이 아래 스크롤(줌아웃 느낌). ImGui는 +가 위로 스크롤.
    // 일반적인 마우스 휠 한칸 = ±1 로 정규화
    let dy = -Math.sign(e.deltaY); // 반전
    let dx = Math.sign(e.deltaX);
    send({ type:'wheel', dx, dy });
    e.preventDefault();
  }, { passive:false });
})();
</script>
""".Replace("%WIDTH%", Constants.WIDTH.ToString())
  .Replace("%HEIGHT%", Constants.HEIGHT.ToString())
  .Replace("%FPS%", Constants.FPS.ToString());

            var body = Encoding.UTF8.GetBytes(html);
            await WriteSimple(stream,
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=utf-8\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                "Connection: close\r\n\r\n");
            await stream.WriteAsync(body, 0, body.Length, ct);
            return;
        }
        else if (path == "/mjpeg" && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            var boundary = "frame";
            await WriteSimple(stream,
                "HTTP/1.1 200 OK\r\n" +
                "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                "Pragma: no-cache\r\n" +
                "Expires: 0\r\n" +
                $"Content-Type: multipart/x-mixed-replace; boundary={boundary}\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Connection: close\r\n\r\n");

            await _imgServer.WaitFirstFrameAsync(ct);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var jpeg = _imgServer.TryGetLastJpeg();
                    if (jpeg is null) { await _imgServer.WaitNextFrameAsync(ct); continue; }

                    await WriteSimple(stream, $"--{boundary}\r\n");
                    await WriteSimple(stream, "Content-Type: image/jpeg\r\n");
                    await WriteSimple(stream, $"Content-Length: {jpeg.Length}\r\n\r\n");
                    await stream.WriteAsync(jpeg, 0, jpeg.Length, ct);
                    await WriteSimple(stream, "\r\n");

                    await _imgServer.WaitNextFrameAsync(ct);
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    Console.WriteLine($"[client] {ex.Message}");
            }
            return;
        }
        ////else if (path == "/input" && string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        //{
        //    if (contentLength <= 0 || !isJson)
        //    {
        //        await WriteSimple(stream,
        //            "HTTP/1.1 400 Bad Request\r\n" +
        //            "Access-Control-Allow-Origin: *\r\n" +
        //            "Content-Length:0\r\n\r\n");
        //        return;
        //    }

        //    // 본문 읽기 (ASCII reader를 쓰고 있으므로 raw stream으로 재독)
        //    var buf = new byte[contentLength];
        //    int read = 0;
        //    while (read < contentLength)
        //    {
        //        int n = await stream.ReadAsync(buf.AsMemory(read, contentLength - read), ct);
        //        if (n <= 0) break;
        //        read += n;
        //    }
        //    if (read != contentLength)
        //    {
        //        await WriteSimple(stream, "HTTP/1.1 400 Bad Request\r\nAccess-Control-Allow-Origin: *\r\nContent-Length:0\r\n\r\n");
        //        return;
        //    }

        //    try
        //    {
        //        var json = Encoding.UTF8.GetString(buf, 0, read);
        //        // 단일 객체/배열 모두 허용
        //        if (json.TrimStart().StartsWith("["))
        //        {
        //            var arr = JsonSerializer.Deserialize<InputEventDto[]>(json);
        //            if (arr != null)
        //                foreach (var d in arr) EnqueueDto(d);
        //        }
        //        else
        //        {
        //            var d = JsonSerializer.Deserialize<InputEventDto>(json);
        //            if (d != null) EnqueueDto(d);
        //        }

        //        await WriteSimple(stream,
        //            "HTTP/1.1 204 No Content\r\n" +
        //            "Access-Control-Allow-Origin: *\r\n" +
        //            "Content-Length:0\r\n\r\n");
        //    }
        //    catch
        //    {
        //        await WriteSimple(stream, "HTTP/1.1 400 Bad Request\r\nAccess-Control-Allow-Origin: *\r\nContent-Length:0\r\n\r\n");
        //    }
        //    return;
        //}

        else
        {
            await WriteSimple(stream, "HTTP/1.1 404 Not Found\r\nContent-Length:0\r\n\r\n");
            return;
        }

        //// 로컬 헬퍼
        //void EnqueueDto(InputEventDto d)
        //{
        //    switch (d.type)
        //    {
        //        case "move":
        //            _imgServer.EnqueueInput(new InputEvent { Type = InputType.Move, X = d.x, Y = d.y });
        //            break;
        //        case "down":
        //            _imgServer.EnqueueInput(new InputEvent { Type = InputType.Down, Button = d.button });
        //            break;
        //        case "up":
        //            _imgServer.EnqueueInput(new InputEvent { Type = InputType.Up, Button = d.button });
        //            break;
        //        case "wheel":
        //            _imgServer.EnqueueInput(new InputEvent { Type = InputType.Wheel, Dx = d.dx, Dy = d.dy });
        //            break;
        //    }
        //}
    }

    private static Task WriteSimple(NetworkStream s, string text)
        => s.WriteAsync(Encoding.ASCII.GetBytes(text), 0, text.Length);

    // 입력 JSON DTO (소문자 필드 매핑)
    private sealed class InputEventDto
    {
        public string type { get; set; } = "";
        public float x { get; set; }
        public float y { get; set; }
        public int button { get; set; }
        public float dx { get; set; }
        public float dy { get; set; }
    }
}

// ====================================================================
// 구현부
// ====================================================================
sealed class OffscreenImGuiServer : IDisposable
{
    private readonly int width, height;
    private GLFWwindowPtr window;
    private GL gl = null!;
    private uint fbo, colorTex, rbo;

    // ImGui/ImPlot/ImNodes
    private ImGuiContextPtr guiContext;
    private ImPlotContextPtr plotContext;
    private ImNodesContextPtr nodesContext;
    private ImGuiIOPtr io;
    private string glslVersion = "#version 150";

    // 프레임 공유: JPEG 한 벌
    private byte[]? lastJpeg;
    private readonly AsyncAutoResetEvent frameReady = new();

    public Action? RenderDelegate { get; init; }
    public bool IsShowImGuiCppDemo { get; set; } = false;
    public bool IsShowImGuiCSharpDemo { get; set; } = false;

    public OffscreenImGuiServer(int w, int h) { width = w; height = h; }

    public void Initialize()
    {
        GLFW.Init();

        // 숨김 창 + GL 3.2+ Core
        GLFW.WindowHint(GLFW.GLFW_VISIBLE, 0);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);

        window = GLFW.CreateWindow(width, height, "offscreen", null, null);
        if (window.IsNull) throw new Exception("CreateWindow failed.");

        GLFW.MakeContextCurrent(window);
        gl = new(new BindingsContext(window));

        // FBO 구성
        fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(GLFramebufferTarget.Framebuffer, fbo);

        colorTex = gl.GenTexture();
        gl.BindTexture(GLTextureTarget.Texture2D, colorTex);
        gl.TexImage2D(GLTextureTarget.Texture2D, 0, GLInternalFormat.Rgba8, width, height, 0, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, IntPtr.Zero);
        gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)GLEnum.Nearest);
        gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)GLEnum.Nearest);
        gl.FramebufferTexture2D(GLFramebufferTarget.Framebuffer, GLFramebufferAttachment.ColorAttachment0, GLTextureTarget.Texture2D, colorTex, 0);

        rbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(GLRenderbufferTarget.Renderbuffer, rbo);
        gl.RenderbufferStorage(GLRenderbufferTarget.Renderbuffer, GLInternalFormat.Depth24Stencil8, width, height);
        gl.FramebufferRenderbuffer(GLFramebufferTarget.Framebuffer, GLFramebufferAttachment.DepthStencilAttachment, GLRenderbufferTarget.Renderbuffer, rbo);

        var status = gl.CheckFramebufferStatus(GLFramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            throw new Exception($"FBO incomplete: 0x{status:X}");

        gl.BindFramebuffer(GLFramebufferTarget.Framebuffer, 0);

        // ImGui 컨텍스트/IO/백엔드
        guiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(guiContext);

        ImPlot.SetImGuiContext(guiContext);
        plotContext = ImPlot.CreateContext();
        ImPlot.SetCurrentContext(plotContext);
        ImPlot.StyleColorsDark(ImPlot.GetStyle());

        ImNodes.SetImGuiContext(guiContext);
        nodesContext = ImNodes.CreateContext();
        ImNodes.SetCurrentContext(nodesContext);
        ImNodes.StyleColorsDark(ImNodes.GetStyle());

        io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        // 폰트(옵션: 필요시 교체)
        using (var builder = new Hexa.NET.ImGui.Utilities.ImGuiFontBuilder())
        {
            builder.AddDefaultFont()
                   .SetOption(cfg => cfg.FontBuilderFlags |= (uint)ImGuiFreeTypeBuilderFlags.LoadColor)
                   .AddFontFromFileTTF("font/NanumGothicCoding.ttf", 13.0f, new uint[] { 0x1, 0x1FFFF })
                   .Build();
        }

        ImGuiImplGLFW.SetCurrentContext(guiContext);
        if (!ImGuiImplGLFW.InitForOpenGL(Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(window), true))
            throw new Exception("ImGui GLFW init failed.");

        ImGuiImplOpenGL3.SetCurrentContext(guiContext);
        if (!ImGuiImplOpenGL3.Init(glslVersion))
            throw new Exception("ImGui OpenGL3 init failed.");
    }

    public void StartRenderLoopAsync(CancellationToken token, int targetFps = 30)
    {
        Initialize();

        var frameMs = Math.Max(1, 1000 / targetFps);
        var sw = new Stopwatch();

        while (!token.IsCancellationRequested)
        {
            sw.Restart();

            // 1) 오프스크린 바인딩
            gl.BindFramebuffer(GLFramebufferTarget.Framebuffer, fbo);
            gl.Viewport(0, 0, width, height);
            gl.ClearColor(0.12f, 0.12f, 0.12f, 1f);
            gl.Clear(GLClearBufferMask.ColorBufferBit | GLClearBufferMask.DepthBufferBit);

            ApplyPendingInputs();

            // 2) ImGui 프레임
            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplGLFW.NewFrame();
            ImGui.NewFrame();

            // 사용자가 넘긴 렌더 델리게이트 호출
            RenderDelegate?.Invoke();

            ImGui.Render();
            ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            // 3) 픽셀 캡처 → JPEG 인코딩
            var jpeg = CaptureJpeg(quality: 85);
            Interlocked.Exchange(ref lastJpeg, jpeg);
            frameReady.Set();

            // 4) 다음 루프까지 대기
            var delay = frameMs - (int)sw.ElapsedMilliseconds;
            Thread.Sleep(Math.Max(0, delay));
        }
    }

    private readonly ConcurrentQueue<InputEvent> _inputs = new();

    public void EnqueueInput(InputEvent e) => _inputs.Enqueue(e);

    private void ApplyPendingInputs()
    {
        var io = ImGui.GetIO();
        while (_inputs.TryDequeue(out var e))
        {
            switch (e.Type)
            {
                case InputType.Move:
                    io.AddMousePosEvent(e.X, e.Y);
                    break;
                case InputType.Down:
                    io.AddMouseButtonEvent(e.Button, true);
                    break;
                case InputType.Up:
                    io.AddMouseButtonEvent(e.Button, false);
                    break;
                case InputType.Wheel:
                    io.AddMouseWheelEvent(e.Dx, e.Dy);
                    break;
            }
        }
    }

    public async ValueTask WaitFirstFrameAsync(CancellationToken ct)
    {
        if (Volatile.Read(ref lastJpeg) is not null) return;
        await frameReady.WaitAsync(ct);
    }

    public async ValueTask WaitNextFrameAsync(CancellationToken ct)
        => await frameReady.WaitAsync(ct);

    public byte[]? TryGetLastJpeg()
        => Volatile.Read(ref lastJpeg);

    private byte[] CaptureJpeg(int quality)
    {
        var byteCount = width * height * 4;
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);

        try
        {
            GCHandle handle = default;
            try
            {
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                gl.ReadBuffer(GLReadBufferMode.ColorAttachment0);
                gl.ReadPixels(0, 0, width, height, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, handle.AddrOfPinnedObject());
            }
            finally
            {
                if (handle.IsAllocated) handle.Free();
                gl.BindFramebuffer(GLFramebufferTarget.Framebuffer, 0);
            }

            using var img = Image.LoadPixelData<Rgba32>(buffer.AsSpan(0, byteCount), width, height);
            img.Mutate(x => x.Flip(FlipMode.Vertical)); // OpenGL은 좌하 원점

            using var ms = new MemoryStream();
            img.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
            return ms.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplGLFW.Shutdown();

        ImPlot.SetCurrentContext(null);
        ImPlot.SetImGuiContext(null);
        ImPlot.DestroyContext(plotContext);

        ImNodes.SetCurrentContext(null);
        ImNodes.SetImGuiContext(null);
        ImNodes.DestroyContext(nodesContext);

        ImGui.SetCurrentContext(null);
        ImGui.DestroyContext(guiContext);

        if (colorTex != 0) gl.DeleteTexture(colorTex);
        if (rbo != 0) gl.DeleteRenderbuffer(rbo);
        if (fbo != 0) gl.DeleteFramebuffer(fbo);

        if (!window.IsNull) { GLFW.DestroyWindow(window); window = GLFWwindowPtr.Null; }
        GLFW.Terminate();
    }
}

// 간단한 AsyncAutoResetEvent
sealed class AsyncAutoResetEvent
{
    private readonly System.Threading.Channels.Channel<bool> _ch =
        System.Threading.Channels.Channel.CreateBounded<bool>(
            new System.Threading.Channels.BoundedChannelOptions(1)
            { SingleReader = false, SingleWriter = false });

    public void Set() => _ch.Writer.TryWrite(true);
    // Fix for CS0029: Ensure the correct type is returned by explicitly awaiting the ValueTask<bool> and returning a ValueTask.
    public ValueTask WaitAsync(CancellationToken ct)
    {
        return new ValueTask(_ch.Reader.ReadAsync(ct).AsTask());
    }
}

enum InputType { Move, Down, Up, Wheel }
sealed class InputEvent
{
    public InputType Type { get; init; }
    public float X { get; init; }     // 화면 좌표
    public float Y { get; init; }
    public int Button { get; init; }  // 0:좌 1:우 2:중
    public float Dx { get; init; }    // 수평 휠
    public float Dy { get; init; }    // 수직 휠
}