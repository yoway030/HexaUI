namespace AsyncDataTable;

using System.Reflection.PortableExecutable;
using System.Threading.Channels;

public sealed class ListActor<T> : IAsyncDisposable
{
    private readonly List<T> _items = new();
    private readonly Channel<IMessage> _channel;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loopTask;

    private interface IMessage
    {
        ValueTask HandleAsync(List<T> items, CancellationToken ct);
    }

    private sealed class ActionMessage : IMessage
    {
        private readonly Action<List<T>> _action;

        public ActionMessage(Action<List<T>> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public ValueTask HandleAsync(List<T> items, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _action(items);

            return ValueTask.CompletedTask;
        }
    }

    private sealed class FuncMessage<TResult> : IMessage
    {
        private readonly Func<List<T>, TResult> _func;
        private readonly TaskCompletionSource<TResult> _tcs;

        public FuncMessage(Func<List<T>, TResult> func, TaskCompletionSource<TResult> tcs)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
            _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        }

        public ValueTask HandleAsync(List<T> items, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                TResult result = _func(items);
                _tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }

            return ValueTask.CompletedTask;
        }
    }

    public ListActor()
    {
        _channel = Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void InitalizeAsync()
    {
        _loopTask = Task.Run(() => WorkAsync(_cts.Token));
    }

    public async Task TryWork(CancellationToken ct = default)
    {
        while (_channel.Reader.TryRead(out var msg))
        {
            await msg.HandleAsync(_items, ct).ConfigureAwait(false);
        }
    }

    private async Task WorkAsync(CancellationToken ct)
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                await TryWork(ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
        }
    }

    private async ValueTask SendAsync(Action<List<T>> action, CancellationToken ct = default)
    {
        var msg = new ActionMessage(action);
        await _channel.Writer.WriteAsync(msg, ct).ConfigureAwait(false);
    }

    private async Task<TResult> AskAsync<TResult>(Func<List<T>, TResult> func, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var msg = new FuncMessage<TResult>(func, tcs);

        await _channel.Writer.WriteAsync(msg, ct).ConfigureAwait(false);
        return await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public ValueTask AddAsync(T item, CancellationToken ct = default)
        => SendAsync(list => list.Add(item), ct);

    public ValueTask AddRangeAsync(IEnumerable<T> items, CancellationToken ct = default)
        => SendAsync(list => list.AddRange(items), ct);

    public ValueTask ClearAsync(CancellationToken ct = default)
        => SendAsync(list => list.Clear(), ct);

    public ValueTask RemoveAtAsync(int index, CancellationToken ct = default)
        => SendAsync(list => list.RemoveAt(index), ct);

    public ValueTask RemoveAsync(Predicate<T> predicate, CancellationToken ct = default)
        => SendAsync(list => list.RemoveAll(predicate), ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => AskAsync(list => list.Count, ct);

    /// <summary>
    /// 내부 리스트의 snapshot을 복사해서 반환.
    /// </summary>
    public Task<IReadOnlyList<T>> GetSnapshotAsync(CancellationToken ct = default)
        => AskAsync<IReadOnlyList<T>>(list => list.ToArray(), ct);

    /// <summary>
    /// 임의의 연산을 Actor 스레드에서 실행하고 결과를 받는 범용 메서드
    /// </summary>
    public Task<TResult> RunAsync<TResult>(Func<List<T>, TResult> func, CancellationToken ct = default)
        => AskAsync(func, ct);

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();

        try
        {
            await _loopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Dispose();
    }
}
