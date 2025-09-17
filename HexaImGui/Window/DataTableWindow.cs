
namespace ELImGui.Window;

using System.Numerics;

public class DataTableWindow<TData> : BaseWindow
{
    public DataTableWindow(string windowName, DataTableRule<TData> rule, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        TableWidget = new DataTableWidget<TData>($"{windowName}#{nameof(DataTableWidget<TData>)}", rule);
    }

    public DataTableWidget<TData> TableWidget { get; set; }

    public override void OnRender(DateTime utcNow, double deltaSec)
    {
        TableWidget.RenderImObject(utcNow, deltaSec);
    }

    public override void OnUpdate(DateTime utcNow, double deltaSec)
    {
        TableWidget.UpdateImObject(utcNow, deltaSec);
    }

    public override void OnWindowFocused()
    {
        TableWidget.OnWindowFocused();
    }

    public void PushData(TData data)
    {
        TableWidget.DataQueue.Enqueue(data);
    }
}
