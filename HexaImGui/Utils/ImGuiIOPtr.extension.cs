using Hexa.NET.ImGui;
using System.Runtime.CompilerServices;

namespace ELImGui.Utils;

public static class ImGuiIOPtrExtensions
{
    public static ref TOut ConfigFlags<TOut>(this ImGuiIOPtr io)
    {
        return ref Unsafe.As<ImGuiConfigFlags, TOut>(ref io.ConfigFlags);
    }
}
