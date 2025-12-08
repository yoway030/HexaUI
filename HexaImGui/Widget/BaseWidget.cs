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

    public void RenderImObject(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        if (IsVisibleImObject == false)
        {
            return;
        }

        OnPrevRender(utcNow, deltaSec, imInternalContext);
        OnRender(utcNow, deltaSec, imInternalContext);
        OnAfterRender(utcNow, deltaSec, imInternalContext);
    }

    public void UpdateImObject(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext)
    {
        OnUpdate(utcNow, deltaSec, imInternalContext);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext);
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }
    public abstract void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext);
    public virtual void OnWindowFocused(BaseWindow ownerWindow) { }
}
