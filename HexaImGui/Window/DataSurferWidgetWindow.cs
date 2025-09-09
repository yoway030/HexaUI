
namespace ELImGui.Window;

using System.Numerics;

public class DataSurferWidgetWindow<TData> : BaseWindow
    where TData : SurfableIndexingData, new()
{
    public DataSurferWidgetWindow(string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        DataSurferWidget = new DataSurferWidget<TData>(WindowName);
    }

    public DataSurferWidget<TData> DataSurferWidget { get; set; }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        DataSurferWidget.RenderImObject(utcNow, deltaSec);
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        DataSurferWidget.UpdateImObject(utcNow, deltaSec);
    }

    public override void OnWindowFocused()
    {
        DataSurferWidget.OnWindowFocused();
    }
}
