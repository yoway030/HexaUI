using HexaUI;
using HexaUI.Input;
using Silk.NET.SDL;

namespace Sample.ImVisualizer;

public unsafe class ImVisualizer : App
{
    public static ImVisualizer Instance => (ImVisualizer)instance;
    public bool Exiting { get; set; } = false;

    private readonly List<Func<Event, bool>> hooks = new();

    override public void RegisterHookImpl(Func<Event, bool> hook)
    {
        hooks.Add(hook);
    }

    override public void RemoveHookImpl(Func<Event, bool> hook)
    {
        hooks.Remove(hook);
    }

    public static void Init(Backend backend)
    {
        instance = new ImVisualizer();

        Backend = backend;
        sdl.SetHint(Sdl.HintMouseFocusClickthrough, "1");
        sdl.SetHint(Sdl.HintMouseAutoCapture, "0");
        sdl.SetHint(Sdl.HintAutoUpdateJoysticks, "1");
        sdl.SetHint(Sdl.HintJoystickHidapiPS4, "1");
        sdl.SetHint(Sdl.HintJoystickHidapiPS4Rumble, "1");
        sdl.SetHint(Sdl.HintJoystickRawinput, "0");
        sdl.Init(Sdl.InitEvents + Sdl.InitVideo + Sdl.InitGamecontroller + Sdl.InitHaptic + Sdl.InitJoystick + Sdl.InitSensor);

        if (backend == Backend.OpenGL)
        {
            sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
            sdl.GLSetAttribute(GLattr.ContextMinorVersion, 3);
            sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.Core);
        }

        Keyboard.Init();
        Mouse.Init();
    }

    public void Run(CoreWindow window)
    {
        mainWindow = window;
        mainWindowId = window.Id;
        window.InitGraphics();

        window.Show();

        PlatformRun();

        window.Dispose();

        sdl.Quit();
    }

    private void PlatformRun()
    {
        Time.Initialize();

        Event evnt;
        while (!Exiting)
        {
            sdl.PumpEvents();
            while (sdl.PollEvent(&evnt) == (int)SdlBool.True)
            {
                for (int i = 0; i < hooks.Count; i++)
                {
                    hooks[i](evnt);
                }

                HandleEvent(evnt);
            }

            mainWindow.Render();

            Keyboard.Flush();
            Mouse.Flush();
            Time.FrameUpdate();
        }
    }

    private void HandleEvent(Event evnt)
    {
        EventType type = (EventType)evnt.Type;
        switch (type)
        {
            case EventType.Windowevent:
                {
                    var even = evnt.Window;
                    if (even.WindowID == mainWindowId)
                    {
                        switch ((WindowEventID)evnt.Window.Event)
                        {
                            case WindowEventID.Close:
                                Exiting = true;
                                break;
                        }

                        mainWindow.ProcessWindowEvent(even);
                    }
                }
                break;

            case EventType.Mousemotion:
                {
                    var even = evnt.Motion;
                    Mouse.OnMotion(even);
                }
                break;

            case EventType.Mousebuttondown:
                {
                    var even = evnt.Button;
                    Mouse.OnButtonDown(even);
                }
                break;

            case EventType.Mousebuttonup:
                {
                    var even = evnt.Button;
                    Mouse.OnButtonUp(even);
                }
                break;

            case EventType.Mousewheel:
                {
                    var even = evnt.Wheel;
                    Mouse.OnWheel(even);
                }
                break;

            case EventType.Keydown:
                {
                    var even = evnt.Key;
                    Keyboard.OnKeyDown(even);
                }
                break;

            case EventType.Keyup:
                {
                    var even = evnt.Key;
                    Keyboard.OnKeyUp(even);
                }
                break;

            case EventType.Textediting:
                break;

            case EventType.Textinput:
                {
                    var even = evnt.Text;
                    Keyboard.OnTextInput(even);
                }
                break;
        }
    }
}