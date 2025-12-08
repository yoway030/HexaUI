namespace ELImGui.Utils;

using Hexa.NET.ImGui;
using System.Text;

public static class ImGuiHelper
{
    public const byte NewLineByte = (byte)'\n';

    public static void HelpMarker(string desc)
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

    public static void HelpMarkerSameLine(string desc)
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
    public static void HelpMarkerSameLine(params string[] descs)
    {
        HelpMarkerSameLine(String.Join("\n", descs));
    }

    public static void SpacingSameLine()
    {
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
    }

    public static void TextUnformattedUntil(string text, byte target)
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

    public static void TextWithTooltip(string text, string tooltip)
    {
        ImGui.TextUnformatted(text);

        if (ImGui.IsItemHovered())
        {
            if (ImGui.BeginTooltip())
            {
                ImGui.TextUnformatted(tooltip);
                ImGui.Separator();
                ImGui.TextUnformatted("Double click to copy to clipboard.");
                ImGui.EndTooltip();
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                ImGui.SetClipboardText(tooltip);
            }
        }
    }
}
