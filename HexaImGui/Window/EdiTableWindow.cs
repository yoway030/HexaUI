
namespace ELImGui.Window;

using System.Numerics;

public class EdiTableWindow<TData> : SingleWidgetWindow<EdiTableWidget<TData>>
{
    public EdiTableWindow(DataTableRule<TData> rule, string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new EdiTableWidget<TData>(rule, $"{windowName}#{nameof(EdiTableWindow<TData>)}", windowName);
    }

    public void AddPost(TData data)
    {
        Widget.AddPost(data);
    }

    public async Task<int> FindAsk(TData data, IInComparer<TData>? comparer = null)
    {
        return await Widget.FindAsk(data, comparer);
    }

    public async Task<bool> UpdateAsk(TData data, IInComparer<TData>? comparer = null)
    {
        return await Widget.UpdateAsk(data, comparer);
    }

    public void UpdateAtPost(int index, TData data)
    {
        Widget.UpdateAtPost(index, data);
    }

    public void ClearDirect()
    {
        Widget.ClearDirect();
    }

    public void RemoveAtPost(int index)
    {
        Widget.RemoveAtPost(index);
    }
}
