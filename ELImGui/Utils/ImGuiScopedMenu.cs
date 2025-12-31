namespace ELImGui.Utils;

using Hexa.NET.ImGui;

public readonly struct ImGuiScopedMenu : IDisposable
{
    public ImGuiScopedMenu(string name)
    {
        BeginSuccess = ImGui.BeginMenu(name);
    }

    public readonly bool BeginSuccess;

    public void Dispose()
    {
        if (BeginSuccess)
        {
            ImGui.EndMenu();
        }
    }
}
