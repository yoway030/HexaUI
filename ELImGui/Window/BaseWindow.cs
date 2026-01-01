namespace ELImGui.Window;

using ELImGui.Utils;
using Hexa.NET.ImGui;
using System.Numerics;

/// <summary>
/// ImGui 윈도우 래핑 기본 클래스.
/// 간단한 윈도우 생성 및 위치/크기 관리 기능을 제공
/// </summary>
public abstract class BaseWindow : IImWindow, IImVisible, IImRenderable, IImUpdatable
{
    public static readonly Vector4 ColorTextHighLight = new(0.0f, 1.0f, 0.0f, 0.5f);

    public BaseWindow(string windowName, Vector2? parentPosition = null)
    {
        WindowName = windowName;
        IsVisibleImObject = true;
        _windowSize = new Vector2(600, 400);
        _windowPosition = parentPosition != null ?
            new Vector2(parentPosition.Value.X + 30, parentPosition.Value.Y + 30) :
            new Vector2(400, 400);
    }

    private bool _isChangingWindowPosSize = false;
    private Vector2 _windowSize;
    private Vector2 _windowPosition;

    public string WindowName { get; init; }
    public bool IsVisibleImObject { get; set; }
    public Vector2 WindowSize
    {
        get => _windowSize;
        set
        {
            _windowSize = value;
            _isChangingWindowPosSize = true;
        }
    }
    public Vector2 WindowPosition
    {
        get => _windowPosition;
        set
        {
            _windowPosition = value;
            _isChangingWindowPosSize = true;
        }
    }

    public ImGuiWindowFlags WindowFlags { get; set; } = ImGuiWindowFlags.None;

    public void RenderImObject(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        OnPrevRender(utcNow, deltaSec, imInternalContext);

        if (IsVisibleImObject == false)
        {
            return;
        }

        ImGui.SetNextWindowPos(_windowPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(_windowSize, ImGuiCond.FirstUseEver);

        if (_isChangingWindowPosSize == true)
        {
            ImGui.SetNextWindowPos(_windowPosition, ImGuiCond.Always);
            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.Always);
            _isChangingWindowPosSize = false;
        }

        using (var imObject = new ImGuiScopedWindow(WindowName, WindowFlags, IsVisibleImObject))
        {
            if (imObject.BeginSuccess)
            {
                _windowPosition = ImGui.GetWindowPos();
                _windowSize = ImGui.GetWindowSize();

                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
                {
                    OnWindowFocused();
                }

                OnRender(utcNow, deltaSec, imInternalContext);
            }

            IsVisibleImObject = imObject.IsVisible;
        }

        OnAfterRender(utcNow, deltaSec, imInternalContext);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext);
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }

    public void UpdateImObject(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        OnPrevUpdate(utcNow, deltaSec, imInternalContext);
        OnUpdate(utcNow, deltaSec, imInternalContext);
    }

    public abstract void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext);
    public virtual void OnPrevUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }

    public virtual void OnWindowFocused() { }
}
