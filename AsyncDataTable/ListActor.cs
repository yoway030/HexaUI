namespace AsyncDataTable;

using System.Threading.Channels;

public class ListActor<TData> : IAsyncDisposable
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
        IExtensionCommand? Extension = null);

    public interface IExtensionCommand
    {
        void Execute(List<TData> list);
    }

    public sealed class AskExtensionCommand<TResult> : IExtensionCommand
    {
        public AskExtensionCommand(Func<List<TData>, TResult> func, TaskCompletionSource<TResult> tcs)
        {
            Func = func;
            Tcs = tcs;
        }

        public Func<List<TData>, TResult> Func { get; }
        public TaskCompletionSource<TResult> Tcs { get; set; }

        public void Execute(List<TData> list)
        {
            try
            {
                var result = Func(list);
                Tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                Tcs.SetException(ex);
            }
        }
    }

    public ListActor(bool useTask = true)
    {
        _channel = Channel.CreateUnbounded<Command>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        if (useTask == true)
        {
            InitAsyncTask();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();

        try
        {
            await _loopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Cancel();
        _cts.Dispose();
    }

    private readonly List<TData> _items = new();
    private readonly Channel<Command> _channel;
    private readonly CancellationTokenSource _cts = new();
    private Task _loopTask = Task.CompletedTask;

    public void InitAsyncTask()
    {
        _loopTask = Task.Run(() => WorkAsync());
    }

    private async Task WorkAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                TryWork();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
        }
    }

    public void TryWork()
    {
        while (_channel.Reader.TryRead(out var cmd))
        {
            _cts.Token.ThrowIfCancellationRequested();

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
    }

    public void DebugPrint()
    {
        Console.WriteLine("Current Items:");
        for (int i = 0; i < _items.Count; i++)
        {
            Console.WriteLine($"[{i}]: {_items[i]}");
        }
    }

    public ValueTask SendCommand(Command cmd)
    {
        return _channel.Writer.WriteAsync(cmd);
    }

    public async Task<TResult> SendExtendCommand<TResult>(Func<List<TData>, TResult> func, TData? target, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var action = new AskExtensionCommand<TResult>(func, tcs);
        var cmd = new Command(CommandType.ExtendCommand, target, null, action);

        await _channel.Writer.WriteAsync(cmd, ct).ConfigureAwait(false);
        return await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
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

    public Task<int> AddAsync(TData data, CancellationToken ct = default)
        => SendExtendCommand(list => AddInternal(data), data, ct);

    public Task<TData> GetAsync(int index, CancellationToken ct = default)
        => SendExtendCommand(list => GetInternal(index), default, ct);

    public Task<List<TData>> SnapshotAsync(CancellationToken ct = default)
        => SendExtendCommand(list => SnapshotInternal(), default, ct);

    private void ExtendCommandInternal(Command cmd)
    {
        if (cmd.Extension is IExtensionCommand)
        {
            cmd.Extension.Execute(_items);
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
}
