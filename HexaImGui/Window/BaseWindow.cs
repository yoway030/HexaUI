using Hexa.NET.ImGui;
using System.Numerics;

namespace HexaImGui.Window;

public abstract class BaseWindow : ImVisualizerWindow
{
    public static readonly Vector4 ColorTextHighLight = new Vector4(0.0f, 1.0f, 0.0f, 0.5f);

    public BaseWindow(string windowName, int windowDepth = 0, Vector2? parentPosition = null)
    {
        WindowName = windowName;
        WindowDepth = windowDepth;
        WindowId = windowDepth == 0 ? $"{WindowName}" : $"{WindowName}#{WindowDepth}";
        IsVisible = true;
        _windowSize = new Vector2(300, 200);
        _windowPosition = parentPosition != null ?
            new Vector2(parentPosition.Value.X, parentPosition.Value.Y) :
            new Vector2(400, 400);
    }

    private bool _isVisible = true;
    private bool _isChangingWindowPosSize = false;
    private Vector2 _windowSize;
    private Vector2 _windowPosition;

    public string WindowName { get; init; }
    public int WindowDepth { get; init; }
    public string WindowId { get; init; }
    public bool IsVisible { get => _isVisible; set => _isVisible = value; }

    public Vector2 WindowSize { get => _windowSize; set => _windowSize = value; }
    public Vector2 WindowPoistion { get => _windowPosition; set => _windowPosition = value; }

    public void SetWindowPosSize(Vector2 position, Vector2 size)
    {
        _windowPosition = position;
        _windowSize = size;
        _isChangingWindowPosSize = true;
    }

    public void RenderVisualizer(DateTime utcNow, double deltaSec)
    {
        OnPrevRender(utcNow, deltaSec);

        if (_isVisible == false)
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

        if (ImGui.Begin(WindowId, ref _isVisible))
        {
            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
            {
                OnWindowFocused();
            }

            OnRender(utcNow, deltaSec);
        }

        // ImGui.Begin의 반환값과 무관하게 ImGui.End를 호출해야 한다.
        ImGui.End();

        OnAfterRender(utcNow, deltaSec);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec);
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec) { }
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec) { }

    public void UpdateVisualizer(DateTime utcNow, double deltaSec)
    {
        OnUpdate(utcNow, deltaSec);
    }

    public abstract void OnUpdate(DateTime utcNow, double deltaSec);

    public virtual void OnWindowFocused() {}
}
