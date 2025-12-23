
namespace ELImGui.Window;

using ELImGui.Widget;
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
        Widgets.RemoveAll(w => w == widget);
    }

    public override void OnPrevRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        base.OnPrevRender(utcNow, deltaSec, imInternalContext);

        foreach (var widget in Widgets)
        {
            widget.OnPrevRender(utcNow, deltaSec, imInternalContext);
        }
    }

    public override void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        throw new NotImplementedException();
    }

    public override void OnAfterRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        base.OnAfterRender(utcNow, deltaSec, imInternalContext);

        foreach (var widget in Widgets)
        {
            widget.OnAfterRender(utcNow, deltaSec, imInternalContext);
        }
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        foreach (var widget in Widgets)
        {
            widget.OnUpdate(utcNow, deltaSec, imInternalContext);
        }
    }

    public override void OnPrevUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        foreach (var widget in Widgets)
        {
            widget.OnPrevUpdate(utcNow, deltaSec, imInternalContext);
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
