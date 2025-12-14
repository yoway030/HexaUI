
namespace ELImGui.Window;

using System.Numerics;

public class DataTableWindow<TData> : SingleWidgetWindow<DataTableWidget<TData>>
{
    public DataTableWindow(DataTableRule<TData> rule, string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new DataTableWidget<TData>(rule, $"{windowName}#{nameof(DataTableWidget<TData>)}", windowName, 100);
    }

    public void PushData(TData data)
    {
        Widget.PushData(data);
    }
}
