
namespace ELImGui.Window;

using ELImGui.Widget;
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

    public override void OnPrevRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        base.OnPrevRender(utcNow, deltaSec, imInternalContext);
        Widget.OnPrevRender(utcNow, deltaSec, imInternalContext);
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        Widget.OnRender(utcNow, deltaSec, imInternalContext);
    }

    public override void OnAfterRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        base.OnAfterRender(utcNow, deltaSec, imInternalContext);
        Widget.OnAfterRender(utcNow, deltaSec, imInternalContext);
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        Widget.OnUpdate(utcNow, deltaSec, imInternalContext);
    }

    public override void OnPrevUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        Widget.OnPrevUpdate(utcNow, deltaSec, imInternalContext);
    }

    public override void OnWindowFocused()
    {
        base.OnWindowFocused();
        Widget.OnWindowFocused(this);
    }
}
