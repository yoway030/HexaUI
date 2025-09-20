
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

    public List<BaseWidget> Widgets { get; set; } = new();

    public void AddWidget(BaseWidget widget)
    {
        Widgets.Add(widget);
    }

    public void RemoveWidget(BaseWidget widget)
    {
        Widgets.Remove(widget);
    }

    public override void OnPrevRender(DateTime utcNow, double deltaSec)
    {
        base.OnPrevRender(utcNow, deltaSec);

        foreach (var widget in Widgets)
        {
            widget.OnPrevRender(utcNow, deltaSec);
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        ImGui.Separator();

        foreach (var widget in Widgets)
        {
            widget.OnRender(utcNow, deltaSec);
            ImGui.Separator();
        }
    }

    public override void OnAfterRender(DateTime utcNow, double deltaSec)
    {
        base.OnAfterRender(utcNow, deltaSec);

        foreach (var widget in Widgets)
        {
            widget.OnAfterRender(utcNow, deltaSec);
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        foreach (var widget in Widgets)
        {
            widget.OnUpdate(utcNow, deltaSec);
        }
    }

    public override void OnWindowFocused()
    {
        base.OnWindowFocused();

        foreach (var widget in Widgets)
        {
            widget.OnWindowFocused(this);
        }
    }
}
