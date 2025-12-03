namespace ELImGui.Utils;

using Hexa.NET.ImGui;
using System.Numerics;

public readonly struct ImGuiScopedChild : IDisposable
{
    public ImGuiScopedChild(string name, Vector2 size)
    {
        BeginSuccess = ImGui.BeginChild(name, size);
    }

    public readonly bool BeginSuccess;

    public void Dispose()
    {
        ImGui.EndChild();
    }
}
