namespace ELImGui.Utils;

using Hexa.NET.ImGui;
using System.Text;
using System.Text.Unicode;

public static class ImGuiHelper
{
    public const byte NewLineByte = (byte)'\n';

    static public void HelpMarker(string desc)
    {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort) && ImGui.BeginTooltip())
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    static public void HelpMarkerSameLine(string desc)
    {
        ImGui.SameLine(0, 0);
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort) && ImGui.BeginTooltip())
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    // string을 여러 개 받을 수 있는 인자 형태 예시 (params 사용)
    static public void HelpMarkerSameLine(params string[] descs)
    {
        HelpMarkerSameLine(String.Join("\n", descs));
    }

    static public void SpacingSameLine()
    {
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
    }

    static public void TextUnformattedUntil(string text, byte target)
    {
        Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetByteCount(text)];
        int written = Encoding.UTF8.GetBytes(text, buffer);
        int index = buffer.IndexOf(target);

        if (index >= 0)
        {
            ImGui.TextUnformatted(buffer, ref buffer[index]);
        }
        else
        {
            ImGui.TextUnformatted(buffer);
        }
    }
}
