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
        _initSize = new Vector2(300, 200);
        _initPosition = parentPosition != null ?
            new Vector2(parentPosition.Value.X, parentPosition.Value.Y) :
            new Vector2(400, 400);
    }

    private bool _isVisible = true;
    private Vector2 _initSize;
    private Vector2 _initPosition;

    public string WindowName { get; init; }
    public int WindowDepth { get; init; }
    public string WindowId { get; init; }
    public bool IsVisible { get => _isVisible; set => _isVisible = value; }

    public Vector2 InitSize { get => _initSize; set => _initSize = value; }
    public Vector2 InitPoistion { get => _initPosition; set => _initPosition = value; }

    public void RenderVisualizer(DateTime utcNow, double deltaSec)
    {
        if (IsVisible == false)
        {
            return;
        }

        OnPrevRender(utcNow, deltaSec);

        ImGui.SetNextWindowPos(_initPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(_initSize, ImGuiCond.FirstUseEver);

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
