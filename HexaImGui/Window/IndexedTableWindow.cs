
namespace ELImGui.Window;

using System.Numerics;

public class IndexedTableWindow<TData> : SingleWidgetWindow<IndexedTableWidget<TData>>
{
    public IndexedTableWindow(DataTableRule<TData> rule, string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new IndexedTableWidget<TData>(rule, $"{windowName}#{nameof(IndexedTableWidget<TData>)}", windowName);
    }

    public void PushData(TData data)
    {
        Widget.PushData(data);
    }
}
