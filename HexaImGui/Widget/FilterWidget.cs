using Hexa.NET.ImGui;
using HexaImGui.Utils;
using System.Numerics;

namespace HexaImGui.Widget;

class FilterWidget : BaseWidget
{
    public static readonly Vector4 HighLightColor = new Vector4(0.0f, 1.0f, 0.0f, 0.5f);

    public FilterWidget(string widgetName, string windowId) : base(widgetName, windowId)
    {
    }

    private string _filterText = string.Empty;
    private bool _filterHighlight = true;

    public string FilterText { get => _filterText; }
    public bool FilterHighlight {  get => _filterHighlight; }
    public bool NowHighlighting { get => _filterHighlight && NowFiltering; }
    public bool NowFiltering { get => string.IsNullOrWhiteSpace(_filterText) == false; }

    public Action? FilteringChangeFunc;

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

        if (ImGui.Checkbox($"Highlight##{WidgetName}#{WindowId}", ref _filterHighlight) == true)
        {
            OnFilteringChange();
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
    }

    public virtual void OnFilteringChange()
    {
        FilteringChangeFunc?.Invoke();
    }
}
