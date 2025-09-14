namespace ELImGui.Window;

using Hexa.NET.ImGui;
using System.Collections;
using System.Runtime.CompilerServices;

public readonly record struct DataTableColumn(
    string Name,
    float Width = 120f,
    ImGuiTableColumnFlags Flags = ImGuiTableColumnFlags.WidthFixed
);

public class DataTableDefine : IEnumerable<DataTableColumn>
{
    public DataTableDefine() : this(new List<DataTableColumn>())
    {
    }

    public DataTableDefine(List<DataTableColumn> columns,
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX)
    {
        Columns = columns;
        TableFlags = tableFlags;
    }

    public List<DataTableColumn> Columns { get; set; } = new List<DataTableColumn>();
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

public abstract class DataTableRow
{
    public abstract string FieldsToString { get; }

    public abstract IEnumerable<Action> GetColumnSetupActions();

    public abstract IEnumerable<Action> GetFieldDrawActions();

    public virtual void RenderTooltip() { }
}

public interface IIndexedDataTableRow
{
    public uint Index { get; set; }
}

public abstract class IndexedDataTableRow : DataTableRow, IIndexedDataTableRow
{
    public uint Index { get; set; }
}

public class DataTableRow<T1> : DataTableRow
{
    public T1 Data1 { get; set; } = default!;

    public override string FieldsToString => $"{Data1}";

    public override IEnumerable<Action> GetColumnSetupActions()
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Action> GetFieldDrawActions()
    {
        yield return () => { ImGui.TextUnformatted($"{Data1}"); };
        yield break;
    }

    public override void RenderTooltip()
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted($"{Data1}");
        ImGui.EndTooltip();
    }
}

public class IndexedDataTableRow<T1> : DataTableRow<T1>, IIndexedDataTableRow
{
    public uint Index { get; set; }
}

public class DataTableRow<T1, T2> : DataTableRow
{
    public T1 Data1 { get; set; } = default!;
    public T2 Data2 { get; set; } = default!;

    public override string FieldsToString => $"{Data1} {Data2}";

    public override IEnumerable<Action> GetColumnSetupActions()
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Action> GetFieldDrawActions()
    {
        yield return () => { ImGui.TextUnformatted($"{Data1}"); };
        yield return () => { ImGui.TextUnformatted($"{Data2}"); };
        yield break;
    }

    public override void RenderTooltip()
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted($"{Data1}");
        ImGui.TextUnformatted($"{Data2}");
        ImGui.EndTooltip();
    }
}

public class IndexedDataTableRow<T1, T2> : DataTableRow<T1, T2>, IIndexedDataTableRow
{
    public uint Index { get; set; }
}

public class DataTableRow<T1, T2, T3> : DataTableRow
{
    public T1 Data1 { get; set; } = default!;
    public T2 Data2 { get; set; } = default!;
    public T3 Data3 { get; set; } = default!;

    public override string FieldsToString => $"{Data1} {Data2} {Data3}";

    public override IEnumerable<Action> GetColumnSetupActions()
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Action> GetFieldDrawActions()
    {
        yield return () => { ImGui.TextUnformatted($"{Data1}"); };
        yield return () => { ImGui.TextUnformatted($"{Data2}"); };
        yield return () => { ImGui.TextUnformatted($"{Data3}"); };
        yield break;
    }

    public override void RenderTooltip()
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted($"{Data1}");
        ImGui.TextUnformatted($"{Data2}");
        ImGui.TextUnformatted($"{Data3}");
        ImGui.EndTooltip();
    }
}

public class IndexedDataTableRow<T1, T2, T3> : DataTableRow<T1, T2, T3>, IIndexedDataTableRow
{
    public uint Index { get; set; }
}

public class DataTableRow<T1, T2, T3, T4> : DataTableRow
{
    public T1 Data1 { get; set; } = default!;
    public T2 Data2 { get; set; } = default!;
    public T3 Data3 { get; set; } = default!;
    public T4 Data4 { get; set; } = default!;

    public override string FieldsToString => $"{Data1} {Data2} {Data3} {Data4}";

    public override IEnumerable<Action> GetColumnSetupActions()
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Action> GetFieldDrawActions()
    {
        yield return () => { ImGui.TextUnformatted($"{Data1}"); };
        yield return () => { ImGui.TextUnformatted($"{Data2}"); };
        yield return () => { ImGui.TextUnformatted($"{Data3}"); };
        yield return () => { ImGui.TextUnformatted($"{Data4}"); };
        yield break;
    }

    public override void RenderTooltip()
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted($"{Data1}");
        ImGui.TextUnformatted($"{Data2}");
        ImGui.TextUnformatted($"{Data3}");
        ImGui.TextUnformatted($"{Data4}");
        ImGui.EndTooltip();
    }
}

public class IndexedDataTableRow<T1, T2, T3, T4> : DataTableRow<T1, T2, T3, T4>, IIndexedDataTableRow
{
    public uint Index { get; set; }
}

public class DataTableRow<T1, T2, T3, T4, T5> : DataTableRow
{
    public T1 Data1 { get; set; } = default!;
    public T2 Data2 { get; set; } = default!;
    public T3 Data3 { get; set; } = default!;
    public T4 Data4 { get; set; } = default!;
    public T5 Data5 { get; set; } = default!;

    public override string FieldsToString => $"{Data1} {Data2} {Data3} {Data4} {Data5}";

    public override IEnumerable<Action> GetColumnSetupActions()
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Action> GetFieldDrawActions()
    {
        yield return () => { ImGui.TextUnformatted($"{Data1}"); };
        yield return () => { ImGui.TextUnformatted($"{Data2}"); };
        yield return () => { ImGui.TextUnformatted($"{Data3}"); };
        yield return () => { ImGui.TextUnformatted($"{Data4}"); };
        yield return () => { ImGui.TextUnformatted($"{Data5}"); };
        yield break;
    }

    public override void RenderTooltip()
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted($"{Data1}");
        ImGui.TextUnformatted($"{Data2}");
        ImGui.TextUnformatted($"{Data3}");
        ImGui.TextUnformatted($"{Data4}");
        ImGui.TextUnformatted($"{Data5}");
        ImGui.EndTooltip();
    }
}

public class IndexedDataTableRow<T1, T2, T3, T4, T5> : DataTableRow<T1, T2, T3, T4, T5>, IIndexedDataTableRow
{
    public uint Index { get; set; }
}