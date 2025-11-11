
namespace ELImGui.Window;

using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;
using System.Numerics;

public class MultiWidgetWindow : BaseWindow
{
    public MultiWidgetWindow(string windowName, Vector2? parentPosition = null)
        : base(windowName, parentPosition)
    {
    }

    public List<(BaseWidget Widget, float HeightRatio)> Widgets { get; set; } = new();

    public void AddWidget(BaseWidget widget, float heightRatio)
    {
        Widgets.Add((widget, heightRatio));
    }

    public void RemoveWidget(BaseWidget widget)
    {
        Widgets.RemoveAll(x => x.Widget == widget);
    }

    public override void OnPrevRender(DateTime utcNow, double deltaSec)
    {
        base.OnPrevRender(utcNow, deltaSec);

        foreach (var widget in Widgets)
        {
            widget.Widget.OnPrevRender(utcNow, deltaSec);
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        ImGui.Separator();

        float totalHeight = Widgets.Sum(x => x.HeightRatio);
        foreach (var widget in Widgets)
        {
            float childHeight = ImGui.GetWindowHeight() * widget.HeightRatio / totalHeight * 0.9f;
            ImGui.BeginChild(widget.Widget.WidgetName + "Region", new Vector2(0.0f, childHeight));
            widget.Widget.OnRender(utcNow, deltaSec);
            ImGui.EndChild();
            ImGui.Separator();
        }
    }

    public override void OnAfterRender(DateTime utcNow, double deltaSec)
    {
        base.OnAfterRender(utcNow, deltaSec);

        foreach (var widget in Widgets)
        {
            widget.Widget.OnAfterRender(utcNow, deltaSec);
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        foreach (var widget in Widgets)
        {
            widget.Widget.OnUpdate(utcNow, deltaSec);
        }
    }

    public override void OnWindowFocused()
    {
        base.OnWindowFocused();

        foreach (var widget in Widgets)
        {
            widget.Widget.OnWindowFocused(this);
        }
    }
}
