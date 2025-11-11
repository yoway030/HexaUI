namespace ELImGui.Utils;

using Hexa.NET.ImGui;

public readonly struct ImGuiScopedId : IDisposable
{
    public ImGuiScopedId(string id)
    {
        Id = id;
        ImGui.PushID(Id);
    }

    public readonly string Id;

    public void Dispose()
    {
        ImGui.PopID();
    }
}
