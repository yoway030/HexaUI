using Hexa.NET.ImGui;

namespace HexaImGui.Window;

public abstract class BaseWindow : ImVisualizerWindow
{
    public BaseWindow(string windowName, int windowDepth = 0)
    {
        WindowName = windowName;
        WindowDepth = windowDepth;
        WindowId = windowDepth == 0 ? $"{WindowName}" : $"{WindowName}#{WindowDepth}";
        IsVisible = true;
    }

    private bool _isVisible = true;

    public string WindowName { get; init; }
    public int WindowDepth { get; init; }
    public string WindowId { get; init; }
    public bool IsVisible { get => _isVisible; set => _isVisible = value; }

    public void RenderVisualizer(DateTime utcNow, double deltaSec)
    {
        if (IsVisible == false)
        {
            return;
        }

        OnPrevRender(utcNow, deltaSec);

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

    public void UpdateVisualizer(DateTime utcNow, double deltaSec)
    {
        OnUpdate(utcNow, deltaSec);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec);
    public abstract void OnUpdate(DateTime utcNow, double deltaSec);

    public virtual void OnWindowFocused() {}
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec) {}
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec) {}
}
