namespace ELImGui.Window;

using Hexa.NET.ImGui;
using System.Runtime.CompilerServices;

public readonly record struct DataTableColumn(
    string Name,
    float Width = 120f,
    ImGuiTableColumnFlags Flags = ImGuiTableColumnFlags.WidthFixed
);

public readonly struct DataTableCellRenderer<T>
{
    public delegate string StringGetter(in T value);
    private readonly StringGetter _stringGetter;

    public delegate void CustomRenderer(in T value);
    private readonly CustomRenderer? _customRenderer;

    public DataTableCellRenderer(StringGetter getter, CustomRenderer? renderer = null)
    {
        _stringGetter = getter;
        _customRenderer = renderer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Render(in T model)
    {
        if (_customRenderer != null)
        {
            _customRenderer(in model);
        }
        else
        {
            ImGui.TextUnformatted(_stringGetter(model));
        }
    }
}

public sealed class DataTableRoleBuilder<T>
{
    private readonly ImGuiTableFlags _tableFlags;
    private readonly List<DataTableColumn> _columns = new();
    private readonly List<DataTableCellRenderer<T>> _renderers = new();

    public DataTableRoleBuilder(ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX)
    {
        _tableFlags = tableFlags;
    }

    public DataTableRoleBuilder<T> AddColumn(
        string name,
        DataTableCellRenderer<T>.StringGetter getter)
    {
        return AddColumn(name, 100f, ImGuiTableColumnFlags.WidthFixed, getter, null);
    }

    public DataTableRoleBuilder<T> AddColumn(
        string name,
        float width,
        DataTableCellRenderer<T>.StringGetter getter,
        DataTableCellRenderer<T>.CustomRenderer? renderer = null)
    {
        return AddColumn(name, width, ImGuiTableColumnFlags.WidthFixed, getter, renderer);
    }

    public DataTableRoleBuilder<T> AddColumn(
        string name,
        float width,
        ImGuiTableColumnFlags flags,
        DataTableCellRenderer<T>.StringGetter getter,
        DataTableCellRenderer<T>.CustomRenderer? renderer = null)
    {
        _columns.Add(new DataTableColumn(name, width, flags));
        _renderers.Add(new DataTableCellRenderer<T>(getter, renderer));
        return this;
    }

    public DataTableRule<T> Build(
        DataTableRule<T>.RendererFunc? tooltipRender = null,
        DataTableRule<T>.RendererFunc? rowHeadRender = null,
        DataTableRule<T>.RendererFunc? rowFootRender = null,
        DataTableRule<T>.RowToStringConverterFunc? getRowToString = null)
        => new(_columns.ToArray(), _renderers.ToArray(), _tableFlags, tooltipRender, rowHeadRender, rowFootRender, getRowToString);
}

public sealed class DataTableRule<T>
{
    public delegate void RendererFunc(in T model);
    public delegate string RowToStringConverterFunc(in T model);

    public DataTableColumn[] Columns { get; }
    public DataTableCellRenderer<T>[] Renderers { get; }
    public ImGuiTableFlags TableFlags { get; }

    public RendererFunc? TooltipRender { get; }
    public RendererFunc? RowHeadRender { get; }
    public RendererFunc? RowFootRender { get; }

    public RowToStringConverterFunc? RowToStringConverter { get; }

    public DataTableRule(
        DataTableColumn[] cols,
        DataTableCellRenderer<T>[] renderers,
        ImGuiTableFlags flags,
        RendererFunc? tooltipRenderer,
        RendererFunc? rowHeadRender,
        RendererFunc? rowFootRender,
        RowToStringConverterFunc? rowStringConverter)
    {
        Columns = cols;
        Renderers = renderers;
        TableFlags = flags;
        TooltipRender = tooltipRenderer;
        RowHeadRender = rowHeadRender;
        RowFootRender = rowFootRender;
        RowToStringConverter = rowStringConverter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetupColumns()
    {
        for (int i = 0; i < Columns.Length; i++)
        {
            ref readonly var c = ref Columns[i];
            ImGui.TableSetupColumn(c.Name, c.Flags, c.Width);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderRow(in T model)
    {
        for (int i = 0; i < Renderers.Length; i++)
        {
            ImGui.TableNextColumn();
            Renderers[i].Render(in model);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderTooltip(in T model)
    {
        TooltipRender?.Invoke(in model);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderRowHead(in T model)
    {
        RowHeadRender?.Invoke(in model);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderRowFoot(in T model)
    {
        RowFootRender?.Invoke(in model);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string RowToString(in T model)
    {
        return RowToStringConverter?.Invoke(in model) ?? String.Empty;
    }
}

public readonly record struct IndexedRow<TRow>(uint Index, TRow RowData);