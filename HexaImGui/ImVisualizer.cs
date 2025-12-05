namespace ELImGui;

using ELImGui.demo;
using ELImGui.Effect;
using ELImGui.Utils;
using ELImGui.Window;
using ELImGui.Core;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Utilities;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;
using Hexa.NET.OpenGL;
using NLog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

public class ImVisualizer
{
    private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static ImVisualizer Instance => _instance;
    protected static ImVisualizer _instance { get; set; } = default!;

    [MemberNotNullWhen(true, nameof(_instance))]
    public static bool CreateInstance()
    {
        if (Instance != null)
        {
            throw new InvalidOperationException("ImVisualizer instance already exists. Use DestroyInstance() before creating a new one.");
        }

        _instance = new ImVisualizer();
        return true;
    }

    public static void DestroyInstance()
    {
        if (_instance != null)
        {
            _instance = null!;
        }
    }

    private const int CheckPointRenewCount = 1000;
    private DateTime _checkPointTime = DateTime.UtcNow;
    private long _checkPointTick = Stopwatch.GetTimestamp();
    private long _lastTick = Stopwatch.GetTimestamp();
    private long _loopCount = 0;

    private ImGuiContextPtr _guiContext;
    private ImPlotContextPtr _plotContext;
    private ImNodesContextPtr _nodesContext;
    private ImGuiIOPtr _io;
    private ImGuiFontBuilder _builder = null!;
    private GLFWwindowPtr _window = null!;
    private GL _gl = null!;

    private HexaDemo _hexaImGuiDemo = new();
    private ImGuiDemo _imGuiDemo = new();
    private bool _isShowImGuiCppDemo = false;
    private bool _isShowImGuiCSharpDemo = false;
    private bool _isShowHexaDemo = false;

    private ImInternalContext _internalContext = new();
    private bool _isInitInternalContext = false;

    public bool IsWindowShouldClose = false;

    public bool Initialize(string windowTitle)
    {
        GLFW.Init();

        string glslVersion = "#version 150";
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);  // 3.2+ only

        GLFW.WindowHint(GLFW.GLFW_FOCUSED, 1);    // Make window focused on start
        GLFW.WindowHint(GLFW.GLFW_RESIZABLE, 1);  // Make window resizable

        _window = GLFW.CreateWindow(1600, 1000, windowTitle, null, null);
        if (_window.IsNull)
        {
            Logger.ForDebugEvent().Log(LogLevel.Error, "Failed to create GLFW window.");
            GLFW.Terminate();
            return false;
        }

        GLFW.MakeContextCurrent(_window);

        _guiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_guiContext);
        ImGuizmo.SetImGuiContext(_guiContext);

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
            .SetOption(config => { config.MergeMode = true; })
            .AddFontFromFileTTF("font/NanumGothicCoding.ttf", 13.0f, [0x1, 0x1FFFF])
            .AddFontFromFileTTF("font/seguiemj.ttf", 13.0f, [0x1F300, 0x1F6FF])
            .Build();

        ImGuiImplGLFW.SetCurrentContext(_guiContext);

        if (!ImGuiImplGLFW.InitForOpenGL(Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(_window), true))
        {
            Logger.ForDebugEvent().Log(LogLevel.Error, "Failed to init ImGui Impl GLFW");
            GLFW.Terminate();
            return false;
        }

        ImGuiImplOpenGL3.SetCurrentContext(_guiContext);
        if (!ImGuiImplOpenGL3.Init(glslVersion))
        {
            Logger.ForDebugEvent().Log(LogLevel.Error, "Failed to init ImGui Impl OpenGL3");
            GLFW.Terminate();
            return false;
        }

        _gl = new(new BindingsContext(_window));
        return true;
    }

    public void SetWindowTitle(string title)
    {
        GLFW.SetWindowTitle(_window, title);
    }

    public void InitializeMainWindows(IEnumerable<BaseWindow> windows)
    {
        foreach (var window in windows)
        {
            _internalContext.MainWindows.Add(window.WindowName, window);
        }
    }

    public void RegisterMainMenu(IEnumerable<IImMenu> menus)
    {
        foreach (var menu in menus)
        {
            _internalContext.MainMenus.Add(menu);
        }
    }

    public void InitializeInternalContext(
        IEnumerable<BaseWindow> windows,
        IEnumerable<IImMenu> menus,
        IEnumerable<ForegroundEffect> foregroundEffects)
    {
        if (_isInitInternalContext == true)
        {
            throw new InvalidOperationException("ImVisualizer InternalContext is already initialized.");
        }

        _isInitInternalContext = true;

        foreach (var window in windows)
        {
            _internalContext.MainWindows.Add(window.WindowName, window);
        }

        foreach (var menu in menus)
        {
            _internalContext.MainMenus.Add(menu);
        }

        foreach (var effect in foregroundEffects)
        {
            _internalContext.ForegroundEffects.Add(effect);
        }
    }

    public void Loop()
    {
        if (_loopCount % CheckPointRenewCount == 0)
        {
            _checkPointTime = DateTime.UtcNow;
            _checkPointTick = Stopwatch.GetTimestamp();
        }

        long currentTick = Stopwatch.GetTimestamp();
        var currentTime = _checkPointTime.AddMilliseconds((currentTick - _checkPointTick) * 1000.0 / Stopwatch.Frequency);
        double deltaSec = (double)(currentTick - _lastTick) / Stopwatch.Frequency;
        _lastTick = currentTick;

        // Poll for and process events
        GLFW.PollEvents();
        IsWindowShouldClose = GLFW.WindowShouldClose(_window) != 0;

        if (GLFW.GetWindowAttrib(_window, GLFW.GLFW_ICONIFIED) != 0)
        {
            ImGuiImplGLFW.Sleep(10);
            return;
        }

        GLFW.MakeContextCurrent(_window);
        _gl.ClearColor(1, 0.8f, 0.75f, 1);
        _gl.Clear(GLClearBufferMask.ColorBufferBit);

        ImGuiImplOpenGL3.NewFrame();
        ImGuiImplGLFW.NewFrame();
        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        RenderMainMenu(currentTime, deltaSec);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
        ImGui.DockSpaceOverViewport(null, ImGuiDockNodeFlags.PassthruCentralNode, null);
        ImGui.PopStyleColor(1);

        RenderBackground();
        RenderDemo();

        // UI 윈도우처리
        RenderWindows(currentTime, deltaSec);

        RenderForegroundEffect(currentTime, deltaSec);

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

        _loopCount++;
        Thread.Sleep(10);
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
        if (_isShowImGuiCSharpDemo == true)
        {
            _imGuiDemo.Draw();
        }

        if (_isShowImGuiCppDemo == true)
        {
            ImGui.ShowDemoWindow();
        }

        if (_isShowHexaDemo == true)
        {
            _hexaImGuiDemo.Draw();
        }
    }

    private void RenderWindows(DateTime utcNow, double deltaSec)
    {
        var mainWindows = _internalContext.MainWindows.Values;
        var subWindows = _internalContext.SubWindows.Values;

        foreach (var window in mainWindows)
        { 
            window.UpdateImObject(utcNow, deltaSec);
        }

        foreach (var window in subWindows)
        {
            window.UpdateImObject(utcNow, deltaSec);
        }

        foreach (var window in mainWindows)
        {
            window.RenderImObject(utcNow, deltaSec, _internalContext);
        }

        foreach (var window in subWindows)
        {
            window.RenderImObject(utcNow, deltaSec, _internalContext);

            if (window.IsVisibleImObject == false)
            {
                _internalContext.SubWindows.Remove(window.WindowName);
            }
        }
    }

    private void RenderMainMenu(DateTime utcNow, double deltaSec)
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Process"))
            {
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
                var mainWindows = _internalContext.MainWindows.Values;

                foreach (var uiWindow in mainWindows)
                {
                    ImGui.Spacing();

                    if (uiWindow is IImVisible { } visibleWindow)
                    {
                        bool isVisible = visibleWindow.IsVisibleImObject;
                        if (ImGui.Checkbox($"{uiWindow.WindowName}##MainMenu", ref isVisible))
                        {
                            visibleWindow.IsVisibleImObject = isVisible;
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled(uiWindow.WindowName);
                    }
                }

                ImGui.EndMenu();
            }

            var uiMenus = _internalContext.MainMenus;
            foreach (var UiMenu in uiMenus.Select(w => w as IImUpdatable))
            {
                if (UiMenu is null)
                {
                    continue;
                }

                UiMenu.UpdateImObject(utcNow, deltaSec);
            }

            foreach (var UiMenu in uiMenus.Select(w => w as IImRenderable))
            {
                if (UiMenu is null)
                {
                    continue;
                }

                UiMenu.RenderImObject(utcNow, deltaSec, _internalContext);
            }

            if (ImGui.BeginMenu("Help"))
            {
                ImGui.Spacing();
                ImGui.Checkbox("Show HexaDemo", ref _isShowHexaDemo);
                ImGui.Spacing();
                ImGui.Checkbox("Show ImGuiDemo CSharp", ref _isShowImGuiCSharpDemo);
                ImGui.Spacing();
                ImGui.Checkbox("Show ImGuiDemo Cpp", ref _isShowImGuiCppDemo);
                ImGui.Spacing();
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private void RenderForegroundEffect(DateTime utcNow, double deltaSec)
    {
        var foregroundEffects = _internalContext.ForegroundEffects.ToArray();
        foreach (var effect in foregroundEffects)
        {
            effect.UpdateImObject(utcNow, deltaSec);

            if (effect.IsEnd == true)
            {
                _internalContext.ForegroundEffects.Remove(effect);
            }
            else if (effect.IsStart == true)
            {
                effect.RenderImObject(utcNow, deltaSec, _internalContext);
            }
        }
    }

    private void RenderBackground()
    {
        const string labelBackground = "VisualizerBackground";

        // 전체 화면 덮기
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.25f, 0.25f, 0.25f, 1.0f)); // 짙은 회색 배경

        using var background = new ImGuiScopedWindow(labelBackground,
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.MenuBar);

        DrawGridBackground(ImGui.GetWindowDrawList(), viewport.Pos, viewport.Size);

        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
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
