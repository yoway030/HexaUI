using Hexa.NET.ImGui;
using ELImGui.Utils;
using System.Numerics;

namespace ELImGui.Widget;

class FilterWidget : BaseWidget
{
    public static readonly Vector4 HighLightColor = new Vector4(0.0f, 1.0f, 0.0f, 0.5f);

    public FilterWidget(string widgetName, string windowId) : base(widgetName, windowId)
    {
    }

    private string _filterText = string.Empty;
    private bool _viewOnlyFiltered = false;

    public string FilterText { get => _filterText; }
    public bool IsFiltering { get => string.IsNullOrWhiteSpace(_filterText) == false; }
    public bool IsOnlyFileterd { get => IsFiltering && _viewOnlyFiltered == true; }

    public Action? FilterChangingFunc;

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        ImGui.Text($"{WidgetName}:");
        ImGuiHelper.SpacingSameLine();

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 20.0f);
        if (ImGui.InputText($"##{WidgetName}#{WindowId}", ref _filterText, 100, ImGuiInputTextFlags.EnterReturnsTrue) == true)
        {
            OnFilteringChange();
        }
        ImGuiHelper.SpacingSameLine();

        if (IsFiltering == false)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Checkbox($"ViewOnlyFiltered##{WidgetName}#{WindowId}", ref _viewOnlyFiltered) == true)
        {
            OnFilteringChange();
        }

        if (IsFiltering == false)
        {
            ImGui.EndDisabled();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }

    public virtual void OnFilteringChange()
    {
        FilterChangingFunc?.Invoke();
    }
}
