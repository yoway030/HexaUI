namespace ELImGui.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class DictionaryActor<TKey, TValue>
    : CollectionActorBase<Dictionary<TKey, TValue>, DictionaryActor<TKey, TValue>.Command>
    where TKey : notnull
{
    public enum CommandType
    {
        Add,
        Update,
        Remove,
        Clear,
        ExtendCommand,
    }

    public readonly record struct Command(
        CommandType CommandType,
        TKey? Key,
        TValue? Value,
        IExtensionCommand<Dictionary<TKey, TValue>>? Extension = null);

    private readonly Dictionary<TKey, TValue> _items = new();

    protected override Dictionary<TKey, TValue> Items => _items;

    public DictionaryActor(bool useTask = true)
        : base(useTask)
    {
    }

    public string DebugString()
    {
        return DebugStringImpl(dict =>
            string.Join(", ", dict.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    protected override ValueTask HandleCommand(in Command cmd)
    {
        switch (cmd.CommandType)
        {
            case CommandType.Add:
                AddInternal(cmd.Key!, cmd.Value!);
                break;

            case CommandType.Update:
                UpdateInternal(cmd.Key!, cmd.Value!);
                break;

            case CommandType.Remove:
                RemoveInternal(cmd.Key!);
                break;

            case CommandType.Clear:
                ClearInternal();
                break;

            case CommandType.ExtendCommand:
                return ExtendCommandInternal(cmd);

            default:
                throw new ArgumentOutOfRangeException();
        }

        return ValueTask.CompletedTask;
    }

    private ValueTask ExtendCommandInternal(in Command cmd)
    {
        if (cmd.Extension is { } ext)
        {
            return ext.ExecuteAsync(_items);
        }

        return ValueTask.CompletedTask;
    }

    private bool AddInternal(TKey key, TValue value)
    {
        _items.Add(key, value);
        return true;
    }

    private void UpdateInternal(TKey key, TValue value)
    {
        _items[key] = value;
    }

    private void RemoveInternal(TKey key)
    {
        _items.Remove(key);
    }

    private void ClearInternal()
    {
        _items.Clear();
    }

    private TValue GetInternal(TKey key)
    {
        return _items[key];
    }

    private Dictionary<TKey, TValue> SnapshotInternal()
    {
        return new Dictionary<TKey, TValue>(_items);
    }

    public void Add(TKey key, TValue value)
        => SendCommand(new Command(CommandType.Add, key, value));

    public void Update(TKey key, TValue value)
        => SendCommand(new Command(CommandType.Update, key, value));

    public void Remove(TKey key)
        => SendCommand(new Command(CommandType.Remove, key, default));

    public void Clear()
        => SendCommand(new Command(CommandType.Clear, default, default));

    public async Task<TResult> AskCommand<TResult>(
        Func<IDictionary<TKey, TValue>, ValueTask<TResult>> func,
        TKey? targetKey,
        CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var action = new AsyncCommand<Dictionary<TKey, TValue>, TResult>(func, tcs);
        var cmd = new Command(CommandType.ExtendCommand, targetKey, default, action);

        await Writer.WriteAsync(cmd, ct).ConfigureAwait(false);
        return await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public Task<bool> AddAsync(TKey key, TValue value, CancellationToken ct = default)
        => AskCommand(_ => ValueTask.FromResult(AddInternal(key, value)), key, ct);

    public Task<TValue> GetAsync(TKey key, CancellationToken ct = default)
        => AskCommand(_ => ValueTask.FromResult(GetInternal(key)), key, ct);

    public Task<Dictionary<TKey, TValue>> SnapshotAsync(CancellationToken ct = default)
        => AskCommand(_ => ValueTask.FromResult(SnapshotInternal()), default, ct);
}