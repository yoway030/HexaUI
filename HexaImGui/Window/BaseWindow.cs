using Hexa.NET.ImGui;

namespace HexaImGui.Window;

public interface IImGuiWindow
{
    void RenderWindow(DateTime utcNow, double deltaSec);
    void UpdateWindow(DateTime utcNow, double deltaSec);
}

public abstract class BaseWindow : IImGuiWindow
{
    public BaseWindow(string windowName, int windowDepth = 0)
    {
        WindowName = windowName;
        WindowDepth = windowDepth;
        WindowId = windowDepth == 0 ? $"{WindowName}" : $"{WindowName}#{WindowDepth}";
    }

    public string WindowName { get; init; }
    public int WindowDepth { get; init; }
    public string WindowId { get; init; }

    public void RenderWindow(DateTime utcNow, double deltaSec)
    {
        OnPrevRender(utcNow, deltaSec);

        if (ImGui.Begin(WindowId))
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

    public void UpdateWindow(DateTime utcNow, double deltaSec)
    {
        OnUpdate(utcNow, deltaSec);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec);
    public abstract void OnUpdate(DateTime utcNow, double deltaSec);
    public virtual void OnWindowFocused()
    {
    }
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec)
    {
    }

    public virtual void OnAfterRender(DateTime utcNow, double deltaSec)
    {
    }
}
