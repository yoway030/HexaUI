
using System.Numerics;

namespace ELImGui.Window;

public class DataSurferWidgetWindow<TData> : BaseWindow
    where TData : SurfableIndexingData, new()
{
    public DataSurferWidgetWindow(string windowName, int windowDepth = 0, Vector2? parentPosition = null) : base(windowName, windowDepth, parentPosition)
    {
        DataSurferWidget = new DataSurferWidget<TData>(WindowId);
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
