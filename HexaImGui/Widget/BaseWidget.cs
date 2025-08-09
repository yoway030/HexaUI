using Hexa.NET.ImGui;

namespace HexaImGui.Widget;

public abstract class BaseWidget
{
    public BaseWidget(string widgetName, string windowId)
    {
        WidgetName = widgetName;
        WindowId = windowId;
    }

    protected bool IsVisible = true;

    public string WidgetName { get; init; }
    public string WindowId { get; init; }

    public void RenderWidget(DateTime utcNow, double deltaSec)
    {
        if (IsVisible == false)
        {
            return;
        }

        OnPrevRender(utcNow, deltaSec);
        OnRender(utcNow, deltaSec);
        OnAfterRender(utcNow, deltaSec);
    }

    public void UpdateWidget(DateTime utcNow, double deltaSec)
    {
        OnUpdate(utcNow, deltaSec);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec);
    public virtual void OnPrevRender(DateTime utcNow, double deltaSec) { }
    public virtual void OnAfterRender(DateTime utcNow, double deltaSec) { }
    public abstract void OnUpdate(DateTime utcNow, double deltaSec);

}
