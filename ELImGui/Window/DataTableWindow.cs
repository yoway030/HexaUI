
namespace ELImGui.Window;

using System.Numerics;

public class DataTableWindow<TData> : SingleWidgetWindow<DataTableWidget<TData>>
{
    public DataTableWindow(DataTableRule<TData> rule, string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new DataTableWidget<TData>(rule, $"{windowName}#{nameof(DataTableWindow<TData>)}", windowName);
    }

    public void AddDataPost(TData data)
    {
        Widget.AddDataPost(data);
    }

    public async Task<int> FindDataAsk(TData data, IInComparer<TData>? comparer = null)
    {
        return await Widget.FindDataAsk(data, comparer);
    }

    public async Task<bool> UpdateDataAsk(TData data, IInComparer<TData>? comparer = null)
    {
        return await Widget.UpdateDataAsk(data, comparer);
    }

    public void UpdateDataAtPost(int index, TData data)
    {
        Widget.UpdateDataAtPost(index, data);
    }

    public void ClearDataDirect()
    {
        Widget.ClearDataDirect();
    }

    public void RemoveDataAtPost(int index)
    {
        Widget.RemoveDataAtPost(index);
    }
}
