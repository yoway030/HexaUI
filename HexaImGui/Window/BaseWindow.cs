using Hexa.NET.ImGui;

namespace HexaImGui.Window;

public interface IImGuiWindow
{
    void RenderWindow();
    void UpdateWindow();
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

    public void RenderWindow()
    {
        OnPrevRender();

        if (ImGui.Begin(WindowId))
        {
            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
            {
                OnWindowFocused();
            }

            OnRender();
        }

        // ImGui.Begin의 반환값과 무관하게 ImGui.End를 호출해야 한다.
        ImGui.End();

        OnAfterRender();
    }

    public void UpdateWindow()
    {
        OnUpdate();
    }

    public abstract void OnRender();
    public abstract void OnUpdate();
    public virtual void OnWindowFocused()
    {
    }
    public virtual void OnPrevRender()
    {
    }

    public virtual void OnAfterRender()
    {
    }
}
