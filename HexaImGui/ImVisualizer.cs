using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Utilities;
using Hexa.NET.OpenGL;
using HexaImGui.demo;
using HexaImGui.Utils;
using System.Numerics;
using System.Runtime.CompilerServices;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

namespace HexaImGui;

public class ImVisualizer
{
    private ImGuiContextPtr _guiContext;
    private ImGuiIOPtr _io;
    private ImGuiFontBuilder _builder = null!;
    private GLFWwindowPtr _window = null!;
    private GL _gl = null!;

    private readonly List<Action> _drawCallbacks = new();
    private readonly List<Action> _menuCallbacks = new();

    private HexaDemo _hexaImGuiDemo = new HexaDemo();
    private ImGuiDemo _imGuiDemo = new ImGuiDemo();

    public bool IsWindowShouldClose = false;
    public bool IsShowImGuiCppDemo = false;
    public bool IsShowImGuiCSharpDemo = false;
    public bool IsShowHexaDemo = false;

    public void RegisterDrawCallback(Action callback)
    {
        if (callback != null)
            _drawCallbacks.Add(callback);
    }

    public void RegisterMenuCallback(Action callback)
    {
        if (callback != null)
            _menuCallbacks.Add(callback);
    }

    public void Initialize()
    {
        GLFW.Init();

        string glslVersion = "#version 150";
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);  // 3.2+ only

        GLFW.WindowHint(GLFW.GLFW_FOCUSED, 1);    // Make window focused on start
        GLFW.WindowHint(GLFW.GLFW_RESIZABLE, 1);  // Make window resizable

        _window = GLFW.CreateWindow(800, 600, "GLFW Example", null, null);
        if (_window.IsNull)
        {
            Console.WriteLine("Failed to create GLFW window.");
            GLFW.Terminate();
            return;
        }

        GLFW.MakeContextCurrent(_window);

        _guiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_guiContext);

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
        while (IsWindowShouldClose == false)
        {
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

                // 외부 등록 매뉴
                _menuCallbacks.ForEach(cb =>
                {
                    if (cb != null)
                    {
                        cb.Invoke();
                    }
                });

                ImGui.EndMainMenuBar();
            }

            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
            ImGui.DockSpaceOverViewport(null, ImGuiDockNodeFlags.PassthruCentralNode, null);
            ImGui.PopStyleColor(1);

            DrawBackground();

            // 외부 콜백 UI
            foreach (var cb in _drawCallbacks)
            {
                cb.Invoke();
            }

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
        }
    }

    public void Cleanup()
    {
        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplGLFW.Shutdown();
        ImGui.DestroyContext();
        _builder.Dispose();
        _gl.Dispose();

        // Clean up and terminate GLFW
        GLFW.DestroyWindow(_window);
        GLFW.Terminate();
    }

    private void DrawBackground()
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

        ImGui.Begin("Background",
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
