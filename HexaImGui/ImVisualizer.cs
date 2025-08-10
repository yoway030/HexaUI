using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Utilities;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;
using Hexa.NET.OpenGL;
using HexaImGui.demo;
using HexaImGui.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

namespace HexaImGui;

public class ImVisualizer
{
    private const int _checkPointRenewCount = 1000;
    private DateTime _checkPointTime = DateTime.UtcNow;
    private long _checkPointTick = Stopwatch.GetTimestamp();
    private long _lastTick = Stopwatch.GetTimestamp();

    private ImGuiContextPtr _guiContext;
    private ImPlotContextPtr _plotContext;
    private ImNodesContextPtr _nodesContext;
    private ImGuiIOPtr _io;
    private ImGuiFontBuilder _builder = null!;
    private GLFWwindowPtr _window = null!;
    private GL _gl = null!;

    public string LabelBackground = "VisualizerBackground";

    private HexaDemo _hexaImGuiDemo = new HexaDemo();
    private ImGuiDemo _imGuiDemo = new ImGuiDemo();

    public ConcurrentDictionary<string /*windowId*/, ImVisualizerWindow> UiWindows = new();
    public ConcurrentDictionary<string, ImVisualizerObject> UiMenus = new();
    public Action? RenderDelegate;

    public bool IsWindowShouldClose = false;
    public bool IsShowImGuiCppDemo = false;
    public bool IsShowImGuiCSharpDemo = false;
    public bool IsShowHexaDemo = false;

    public void Initialize()
    {
        GLFW.Init();

        string glslVersion = "#version 150";
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);  // 3.2+ only

        GLFW.WindowHint(GLFW.GLFW_FOCUSED, 1);    // Make window focused on start
        GLFW.WindowHint(GLFW.GLFW_RESIZABLE, 1);  // Make window resizable

        _window = GLFW.CreateWindow(1024, 768, "GLFW Example", null, null);
        if (_window.IsNull)
        {
            Console.WriteLine("Failed to create GLFW window.");
            GLFW.Terminate();
            return;
        }

        GLFW.MakeContextCurrent(_window);

        _guiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_guiContext);

        ImPlot.SetImGuiContext(_guiContext);
        _plotContext = ImPlot.CreateContext();
        ImPlot.SetCurrentContext(_plotContext);
        ImPlot.StyleColorsDark(ImPlot.GetStyle());

        ImNodes.SetImGuiContext(_guiContext);
        _nodesContext = ImNodes.CreateContext();
        ImNodes.SetCurrentContext(_nodesContext);
        ImNodes.StyleColorsDark(ImNodes.GetStyle());

        // Setup ImGui config.
        _io = ImGui.GetIO();
        _io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
        _io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
        _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
        _io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // Enable Multi-Viewport / Platform Windows
        _io.ConfigViewportsNoAutoMerge = false;
        _io.ConfigViewportsNoTaskBarIcon = false;

        // OPTIONAL: For custom fonts and icon fonts.
        _builder = new();
        _builder
            .AddDefaultFont()
            .SetOption(config => { config.FontBuilderFlags |= (uint)ImGuiFreeTypeBuilderFlags.LoadColor; })
            .AddFontFromFileTTF("font/NanumGothicCoding.ttf", 13.0f, [0x1, 0x1FFFF])
            .AddFontFromFileTTF("font/seguiemj.ttf", 13.0f, [0x1F300, 0x1F6FF])
            .SetOption(cfg => cfg.FontBuilderFlags |= (uint)ImGuiFreeTypeBuilderFlags.LoadColor)
            .Build();

        ImGuiImplGLFW.SetCurrentContext(_guiContext);

        if (!ImGuiImplGLFW.InitForOpenGL(Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(_window), true))
        {
            Console.WriteLine("Failed to init ImGui Impl GLFW");
            GLFW.Terminate();
            return;
        }

        ImGuiImplOpenGL3.SetCurrentContext(_guiContext);
        if (!ImGuiImplOpenGL3.Init(glslVersion))
        {
            Console.WriteLine("Failed to init ImGui Impl OpenGL3");
            GLFW.Terminate();
            return;
        }

        _gl = new(new BindingsContext(_window));
    }

    public void Loop()
    {
        int loopCount = 0;

        while (IsWindowShouldClose == false)
        {
            if (loopCount % _checkPointRenewCount == 0)
            {
                _checkPointTime = DateTime.UtcNow;
                _checkPointTick = Stopwatch.GetTimestamp();
            }

            var currentTick = Stopwatch.GetTimestamp();
            var currentTime = _checkPointTime.AddMilliseconds((currentTick - _checkPointTick) * 1000.0 / Stopwatch.Frequency);
            var deltaSec = (double)(currentTick - _lastTick) / Stopwatch.Frequency;
            _lastTick = currentTick;

            // Poll for and process events
            GLFW.PollEvents();
            IsWindowShouldClose = GLFW.WindowShouldClose(_window) != 0;

            if (GLFW.GetWindowAttrib(_window, GLFW.GLFW_ICONIFIED) != 0)
            {
                ImGuiImplGLFW.Sleep(10);
                continue;
            }

            GLFW.MakeContextCurrent(_window);
            _gl.ClearColor(1, 0.8f, 0.75f, 1);
            _gl.Clear(GLClearBufferMask.ColorBufferBit);

            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplGLFW.NewFrame();
            ImGui.NewFrame();

            RenderMainMenu(currentTime, deltaSec);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
            ImGui.DockSpaceOverViewport(null, ImGuiDockNodeFlags.PassthruCentralNode, null);
            ImGui.PopStyleColor(1);

            RenderBackground();
            RenderDemo();

            // UI 윈도우처리
            RenderWindows(currentTime, deltaSec);

            RenderDelegate?.Invoke();

            ImGui.Render();
            ImGui.EndFrame();

            GLFW.MakeContextCurrent(_window);
            ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            if ((_io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }

            GLFW.MakeContextCurrent(_window);

            // Swap front and back buffers (double buffering)
            GLFW.SwapBuffers(_window);

            loopCount++;
            Thread.Sleep(10);
        }
    }

    public void Cleanup()
    {
        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplGLFW.Shutdown();

        ImPlot.SetCurrentContext(null);
        ImPlot.SetImGuiContext(null);
        ImPlot.DestroyContext(_plotContext);

        ImNodes.SetCurrentContext(null);
        ImNodes.SetImGuiContext(null);
        ImNodes.DestroyContext(_nodesContext);

        ImGui.SetCurrentContext(null);
        ImGui.DestroyContext(_guiContext);

        _builder.Dispose();
        _gl.Dispose();

        // Clean up and terminate GLFW
        GLFW.DestroyWindow(_window);
        GLFW.Terminate();
    }

    private void RenderDemo()
    {
        if (IsShowImGuiCSharpDemo == true)
        {
            _imGuiDemo.Draw();
        }

        if (IsShowImGuiCppDemo == true)
        {
            ImGui.ShowDemoWindow();
        }

        if (IsShowHexaDemo == true)
        {
            _hexaImGuiDemo.Draw();
        }
    }

    private void RenderWindows(DateTime utcNow, double deltaSec)
    {
        var uiWindows = UiWindows.Values.ToArray();
        foreach (var uiWindow in uiWindows)
        {
            uiWindow.UpdateVisualizer(utcNow, deltaSec);
            uiWindow.RenderVisualizer(utcNow, deltaSec);
        }
    }

    private void RenderMainMenu(DateTime utcNow, double deltaSec)
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Process"))
            {
                ImGui.Spacing();
                ImGui.Checkbox("Show HexaDemo", ref IsShowHexaDemo);
                ImGui.Spacing();
                ImGui.Checkbox("Show ImGuiDemo CSharp", ref IsShowImGuiCSharpDemo);
                ImGui.Spacing();
                ImGui.Checkbox("Show ImGuiDemo Cpp", ref IsShowImGuiCppDemo);

                ImGui.Spacing();
                if (ImGui.MenuItem("Exit"))
                {
                    IsWindowShouldClose = true;
                }
                ImGui.Spacing();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Windows"))
            {
                var uiWindows = UiWindows.Values.ToArray();
                foreach (var uiWindow in uiWindows)
                {
                    ImGui.Spacing();

                    bool isVisible = uiWindow.IsVisible;
                    if (ImGui.Checkbox($"{uiWindow.WindowName}##MainMenu", ref isVisible))
                    {
                        uiWindow.IsVisible = isVisible;
                    }
                }
                ImGui.EndMenu();
            }


            var uiMenus = UiMenus.Values.ToArray();
            foreach (var uiMenu in uiMenus)
            {
                uiMenu.UpdateVisualizer(utcNow, deltaSec);
                uiMenu.RenderVisualizer(utcNow, deltaSec);
            }

            ImGui.EndMainMenuBar();
        }
    }

    private void RenderBackground()
    {
        // 전체 화면 덮기
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.25f, 0.25f, 0.25f, 1.0f)); // 짙은 회색 배경

        ImGui.Begin(LabelBackground,
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.MenuBar);

        DrawGridBackground(ImGui.GetWindowDrawList(), viewport.Pos, viewport.Size);

        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();

        ImGui.End(); // Background
    }

    private void DrawGridBackground(ImDrawListPtr drawList, Vector2 origin, Vector2 size)
    {
        const float gridSpacing = 32.0f;
        var gridColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        uint gridColorU32 = ImGui.ColorConvertFloat4ToU32(gridColor);

        for (float x = origin.X; x < origin.X + size.X; x += gridSpacing)
        {
            drawList.AddLine(new Vector2(x, origin.Y), new Vector2(x, origin.Y + size.Y), gridColorU32, 1.0f);
        }

        for (float y = origin.Y; y < origin.Y + size.Y; y += gridSpacing)
        {
            drawList.AddLine(new Vector2(origin.X, y), new Vector2(origin.X + size.X, y), gridColorU32, 1.0f);
        }
    }
}
