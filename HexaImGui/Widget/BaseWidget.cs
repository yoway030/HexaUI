namespace ELImGui.Widget;

public abstract class BaseWidget : IImWidget, IImVisible, IImRenderable, IImUpdatable
{
    public BaseWidget(string widgetName, string parentWindowName)
    {
        WidgetName = widgetName;
        ParentWindowName = parentWindowName;
    }

    public string WidgetName { get; init; }
    public string ParentWindowName { get; init; }
    public bool IsVisibleImObject { get; set; } = true;

    public void RenderImObject(DateTime utcNow, double deltaSec)
    {
        if (IsVisibleImObject == false)
        {
            return;
        }

        OnPrevRender(utcNow, deltaSec);
        OnRender(utcNow, deltaSec);
        OnAfterRender(utcNow, deltaSec);
    }

    public void UpdateImObject(DateTime utcNow, double deltaSec)
    {
        OnUpdate(utcNow, deltaSec);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec);
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec) { }
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec) { }
    public abstract void OnUpdate(DateTime utcNow, double deltaSec);
}
