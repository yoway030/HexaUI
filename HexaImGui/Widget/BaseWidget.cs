namespace ELImGui.Widget;

using ELImGui.Window;

public abstract class BaseWidget : IImWidget, IImVisible, IImRenderable, IImUpdatable
{
    public BaseWidget() : this($"{nameof(BaseWidget)}", String.Empty)
    {
    }

    public BaseWidget(string widgetName, string ownerWindowName)
    {
        WidgetName = widgetName;
        OwnerWindowName = ownerWindowName;
    }

    public string WidgetName { get; set; }
    public string OwnerWindowName { get; set; }
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
    public virtual void OnWindowFocused(BaseWindow ownerWindow) { }
}
