namespace ELImGui.Widget;

using ELImGui.Window;

/// <summary>
/// ImGui의 버튼 등 Widget을 사용하여 쓰기 편리하게 래핑된 Widget들의 기본 클래스
/// </summary>
public abstract class BaseWidget : IImWidget, IImVisible, IImRenderable, IImUpdatable
{
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
        OnPrevUpdate(utcNow, deltaSec, imInternalContext);
        OnUpdate(utcNow, deltaSec, imInternalContext);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext);
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }
    public abstract void OnUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext);
    public virtual void OnPrevUpdate(DateTime utcNow, double deltaSec, ImInternalContext imInternalContext) { }
    public virtual void OnWindowFocused(BaseWindow ownerWindow) { }
}
