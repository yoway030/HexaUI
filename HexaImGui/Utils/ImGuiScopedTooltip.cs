namespace ELImGui.Utils;

using Hexa.NET.ImGui;

public readonly struct ImGuiScopedTooltip : IDisposable
{
    public ImGuiScopedTooltip()
    {
        BeginSuccess = ImGui.BeginTooltip();
    }

    public readonly bool BeginSuccess;

    public void Dispose()
    {
        ImGui.EndTooltip();
    }
}
