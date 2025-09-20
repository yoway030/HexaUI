
namespace ELImGui.Window;

using System.Numerics;

public class DataTableWindow<TData> : SingleWidgetWindow<DataTableWidget<TData>>
{
    public DataTableWindow(string windowName, DataTableRule<TData> rule, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new DataTableWidget<TData>($"{windowName}#{nameof(DataTableWidget<TData>)}", rule);
    }

    public void PushData(TData data)
    {
        Widget.DataQueue.Enqueue(data);
    }
}
