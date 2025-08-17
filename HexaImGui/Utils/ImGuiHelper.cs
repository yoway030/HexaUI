using Hexa.NET.ImGui;

namespace ELImGui.Utils;

static class ImGuiHelper
{
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
        ImGui.SameLine();
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
        HelpMarkerSameLine(string.Join("\n", descs));
    }

    static public void SpacingSameLine()
    {
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
    }
}
