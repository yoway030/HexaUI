
namespace ELImGui.Window;

using System.Numerics;

public class EdiTableWindow<TData> : SingleWidgetWindow<EdiTableWidget<TData>>
{
    public EdiTableWindow(DataTableRule<TData> rule, string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new EdiTableWidget<TData>(rule, $"{windowName}#{nameof(EdiTableWindow<TData>)}", windowName);
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
