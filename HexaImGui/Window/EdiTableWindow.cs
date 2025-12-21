
namespace ELImGui.Window;

using System.Numerics;

public class EdiTableWindow<TData> : SingleWidgetWindow<EdiTableWidget<TData>>
{
    public EdiTableWindow(DataTableRule<TData> rule, string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new EdiTableWidget<TData>(rule, $"{windowName}#{nameof(EdiTableWindow<TData>)}", windowName);
    }

    public uint AddData(TData data)
    {
        return Widget.AddData(data);
    }

    public async Task<uint> FindData(TData data)
    {
        return await Widget.FindData(data);
    }

    public async Task<bool> UpdateData(uint index, TData data)
    {
        return await Widget.UpdateIndexedData(index, data);
    }

    public void ClearData()
    {
        Widget.ClearData();
    }

    public async Task RemoveIndex(uint index)
    {
        await Widget.RemoveIndex(index);
    }
}
