namespace ELImGui.Utils;

using Hexa.NET.ImGui;

public readonly struct ImGuiScopedWindow : IDisposable
{
    public ImGuiScopedWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool isVisible = default)
    {
        Name = name;
        IsVisible = isVisible;
        BeginSuccess = ImGui.Begin(Name, ref IsVisible, flags);
    }

    public readonly string Name;
    public readonly bool BeginSuccess;
    public readonly bool IsVisible;

    public void Dispose()
    {
        // NOTE: ImGui.Begin의 반환값과 무관하게 ImGui.End를 호출해야 한다.
        ImGui.End();
    }
}
