namespace AsyncDataTable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ListActor<TData>
    : CollectionActorBase<List<TData>, ListActor<TData>.Command>
{
    public enum CommandType
    {
        Add,
        Insert,
        Remove,
        RemoveAt,
        Update,
        Clear,
        ExtendCommand
    }

    public readonly record struct Command(
        CommandType CommandType,
        TData? Item,
        int? Index = null,
        IExtensionCommand<List<TData>>? Extension = null);

    private readonly List<TData> _items = new();

    protected override List<TData> Items => _items;

    public ListActor(bool useTask = true)
        : base(useTask)
    {
    }

    public string DebugString()
    {
        return DebugStringImpl(list =>
            string.Join(",", list.Select(i => i?.ToString())));
    }

    protected override void HandleCommand(in Command cmd)
    {
        switch (cmd.CommandType)
        {
            case CommandType.Add:
                AddInternal(cmd.Item!);
                break;

            case CommandType.Insert:
                InsertInternal(cmd.Index!.Value, cmd.Item!);
                break;

            case CommandType.Remove:
                RemoveInternal(cmd.Item!);
                break;

            case CommandType.RemoveAt:
                RemoveAtInternal(cmd.Index!.Value);
                break;

            case CommandType.Update:
                UpdateInternal(cmd.Index!.Value, cmd.Item!);
                break;

            case CommandType.Clear:
                ClearInternal();
                break;

            case CommandType.ExtendCommand:
                ExtendCommandInternal(cmd);
                break;
        }
    }

    private void ExtendCommandInternal(in Command cmd)
    {
        if (cmd.Extension is { } ext)
        {
            ext.Execute(_items);
        }
    }

    private int AddInternal(TData data)
    {
        _items.Add(data);
        return _items.Count - 1;
    }

    private void InsertInternal(int index, TData data)
    {
        _items.Insert(index, data);
    }

    private void RemoveInternal(TData data)
    {
        _items.Remove(data);
    }

    private void RemoveAtInternal(int index)
    {
        _items.RemoveAt(index);
    }

    private void UpdateInternal(int index, TData data)
    {
        _items[index] = data;
    }

    private void ClearInternal()
    {
        _items.Clear();
    }

    private TData GetInternal(int index)
    {
        return _items[index];
    }

    private List<TData> SnapshotInternal()
    {
        return new(_items);
    }

    public ValueTask Add(TData data)
        => SendCommand(new Command(CommandType.Add, data, null));

    public ValueTask Insert(int index, TData data)
        => SendCommand(new Command(CommandType.Insert, data, index));

    public ValueTask Remove(TData data)
        => SendCommand(new Command(CommandType.Remove, data, null));

    public ValueTask RemoveAt(int index)
        => SendCommand(new Command(CommandType.RemoveAt, default, index));

    public ValueTask Update(int index, TData data)
        => SendCommand(new Command(CommandType.Update, data, index));

    public ValueTask Clear()
        => SendCommand(new Command(CommandType.Clear, default, null));

    public async Task<TResult> SendExtendCommand<TResult>(
        Func<List<TData>, TResult> func,
        TData? target,
        CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var action = new AskExtensionCommand<List<TData>, TResult>(func, tcs);
        var cmd = new Command(CommandType.ExtendCommand, target, null, action);

        await Writer.WriteAsync(cmd, ct).ConfigureAwait(false);
        return await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public Task<int> AddAsync(TData data, CancellationToken ct = default)
        => SendExtendCommand(list => AddInternal(data), data, ct);

    public Task<TData> GetAsync(int index, CancellationToken ct = default)
        => SendExtendCommand(list => GetInternal(index), default, ct);

    public Task<List<TData>> SnapshotAsync(CancellationToken ct = default)
        => SendExtendCommand(list => SnapshotInternal(), default, ct);
}
