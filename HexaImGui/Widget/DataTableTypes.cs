namespace ELImGui.Window;

using Hexa.NET.ImGui;
using System.Collections;

public record struct DataTableColumn
{
    public DataTableColumn(string name, int width, ImGuiTableColumnFlags flags)
    {
        Name = name;
        Width = width;
        Flags = flags;
    }

    public string Name { get; set; } = "";
    public int Width { get; set; } = 100;
    public ImGuiTableColumnFlags Flags { get; set; } = ImGuiTableColumnFlags.WidthFixed;
}

public class DataTableDefine : IEnumerable<DataTableColumn>
{
    public DataTableDefine()
    {
        Columns = new List<DataTableColumn>();
    }

    public DataTableDefine(List<DataTableColumn> columns)
    {
        Columns = columns;
    }

    public List<DataTableColumn> Columns { get; set; }
    public ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;

    public IEnumerator<DataTableColumn> GetEnumerator()
    {
        return Columns.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Columns.GetEnumerator();
    }
}

public abstract class ViewableData
{
    public abstract IEnumerable<Action> GetColumnSetupActions();

    public abstract IEnumerable<Action> GetFieldDrawActions();

    public virtual void RenderTooltip() { }

    public abstract string FieldsToString { get; }
}

public abstract class SurfableIndexingData : ViewableData
{
    protected uint _index;
    protected string _cachedIndexString = string.Empty;

    public uint Index
    {
        get => _index;
        set
        {
            _index = value;
            _cachedIndexString = $"{_index}";
        }
    }

    public string IndexString => _cachedIndexString;
}