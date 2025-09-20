namespace ELImGui.Widget;

using System.Numerics;
using Hexa.NET.ImGui;
using ELImGui.Utils;

class FilterWidget : BaseWidget
{
    public static readonly Vector4 HighLightColor = new(0.0f, 1.0f, 0.0f, 0.5f);

    public FilterWidget(string widgetName, string parentWindowName) : base(widgetName, parentWindowName)
    {
    }

    private string _filterText = String.Empty;
    private bool _viewOnlyFiltered = false;

    public string FilterText => _filterText;
    public bool IsFiltering => String.IsNullOrWhiteSpace(_filterText) == false;
    public bool IsOnlyFileterd => IsFiltering && _viewOnlyFiltered == true;

    public Action? FilterChangingFunc;

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        ImGui.Text($"{WidgetName}:");
        ImGuiHelper.SpacingSameLine();

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 20.0f);
        if (ImGui.InputText($"##{WidgetName}#{OwnerWindowName}", ref _filterText, 100, ImGuiInputTextFlags.EnterReturnsTrue) == true)
        {
            OnFilteringChange();
        }

        ImGuiHelper.SpacingSameLine();

        if (IsFiltering == false)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Checkbox($"ViewOnlyFiltered##{WidgetName}#{OwnerWindowName}", ref _viewOnlyFiltered) == true)
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
