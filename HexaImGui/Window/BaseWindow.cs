namespace ELImGui.Window;

using Hexa.NET.ImGui;
using System.Numerics;

public abstract class BaseWindow : IImWindow, IImVisible, IImRenderable, IImUpdatable
{
    public static readonly Vector4 ColorTextHighLight = new(0.0f, 1.0f, 0.0f, 0.5f);

    public BaseWindow(string windowName, Vector2? parentPosition = null)
    {
        WindowName = windowName;
        IsVisibleImObject = true;
        _windowSize = new Vector2(300, 200);
        _windowPosition = parentPosition != null ?
            new Vector2(parentPosition.Value.X, parentPosition.Value.Y) :
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

    public void RenderImObject(DateTime utcNow, double deltaSec)
    {
        OnPrevRender(utcNow, deltaSec);

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

        bool isVisible = IsVisibleImObject;
        if (ImGui.Begin(WindowName, ref isVisible, WindowFlags))
        {
            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
            {
                OnWindowFocused();
            }

            OnRender(utcNow, deltaSec);
        }

        // ImGui.Begin의 반환값과 무관하게 ImGui.End를 호출해야 한다.
        ImGui.End();
        IsVisibleImObject = isVisible;

        OnAfterRender(utcNow, deltaSec);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec);
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec) { }
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec) { }

    public void UpdateImObject(DateTime utcNow, double deltaSec)
    {
        OnUpdate(utcNow, deltaSec);
    }

    public abstract void OnUpdate(DateTime utcNow, double deltaSec);

    public virtual void OnWindowFocused() { }
}
