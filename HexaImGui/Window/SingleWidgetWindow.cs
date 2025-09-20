
namespace ELImGui.Window;

using ELImGui.Widget;
using Hexa.NET.ImGui;
using System;
using System.Numerics;

public class SingleWidgetWindow<TWidget> : BaseWindow
    where TWidget : BaseWidget
{
    public SingleWidgetWindow(string windowName, Vector2? parentPosition = null)
        : base(windowName, parentPosition)
    {
    }

    public TWidget Widget { get; set; } = null!;

    public virtual void InitializeWidget(TWidget widget)
    {
        Widget = widget;
    }

    public override void OnPrevRender(DateTime utcNow, double deltaSec)
    {
        base.OnPrevRender(utcNow, deltaSec);
        Widget.OnPrevRender(utcNow, deltaSec);
    }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        Widget.OnRender(utcNow, deltaSec);
    }

    public override void OnAfterRender(DateTime utcNow, double deltaSec)
    {
        base.OnAfterRender(utcNow, deltaSec);
        Widget.OnAfterRender(utcNow, deltaSec);
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        Widget.OnUpdate(utcNow, deltaSec);
    }

    public override void OnWindowFocused()
    {
        base.OnWindowFocused();
        Widget.OnWindowFocused(this);
    }
}
