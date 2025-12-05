
namespace ELImGui.Window;

using ELImGui.Utils;
using Hexa.NET.ImGui;

public class SampleWindow : IImWindow, IImRenderable
{
    public SampleWindow()
    {
        WindowName = nameof(SampleWindow);
        WindowDepth = 0;
        WindowId = WindowName;
    }

    public string WindowName { get; init; }
    public int WindowDepth { get; init; }
    public string WindowId { get; init; }

    private int _butonClick = 0;

    public void RenderImObject(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        using var imObject = new ImGuiScopedWindow(WindowId);
        if (imObject.BeginSuccess)
        {
            if (ImGui.Button("button1"))
            {
                _butonClick++;
            }

            ImGui.SameLine();

            if (_butonClick > 0)
            {
                ImGui.Text($"hello {_butonClick}");
            }
        }
    }
}
