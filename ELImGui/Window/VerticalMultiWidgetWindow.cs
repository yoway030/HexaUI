
namespace ELImGui.Window;

using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;
using System.Numerics;

public class VerticalMultiWidgetWindow : MultiWidgetWindow
{
    public VerticalMultiWidgetWindow(string windowName, Vector2? parentPosition = null)
        : base(windowName, parentPosition)
    {
    }

    private Dictionary<BaseWidget, float> _heightRatios = new();

    public void AddWidget(BaseWidget widget, float heightRatio)
    {
        base.AddWidget(widget);
        _heightRatios.Add(widget, heightRatio);
    }

    public new void RemoveWidget(BaseWidget widget)
    {
        base.RemoveWidget(widget);
        _heightRatios.Remove(widget);
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        float totalHeight = _heightRatios.Sum(x => x.Value);

        foreach (var widget in Widgets)
        {
            float widgetHeightRatio = _heightRatios[widget];
            float childHeight = ImGui.GetWindowHeight() * (widgetHeightRatio / totalHeight);

            ImGui.BeginChild(widget.WidgetName + "Region", new Vector2(0.0f, childHeight));
            widget.OnRender(utcNow, deltaSec, imInternalContext);
            ImGui.EndChild();
        }
    }
}
