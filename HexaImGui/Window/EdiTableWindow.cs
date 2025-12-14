
namespace ELImGui.Window;

using System.Numerics;

public class EdiTableWindow<TData> : SingleWidgetWindow<EdiTableWidget<TData>>
{
    public EdiTableWindow(DataTableRule<TData> rule, string windowName, Vector2? parentPosition = null) : base(windowName, parentPosition)
    {
        Widget = new EdiTableWidget<TData>(rule, $"{windowName}#{nameof(EdiTableWindow<TData>)}", windowName);
    }

    public void AddData(TData data)
    {
        Widget.AddData(data);
    }

    public async Task UpdateData(uint index, TData data)
    {
        await Widget.UpdateData(index, data);
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
