namespace AsyncDataTable;

using System;
using System.Threading.Channels;

public class CmdList<TData> : IAsyncDisposable, IDisposable
    where TData : IEquatable<TData>
{
    public CmdList()
    {
        _cmdChannel = Channel.CreateUnbounded<Action>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly Channel<Action> _cmdChannel;

    private Task? _processorTask;
    private bool _disposed;

    private readonly List<TData> _datas = new();

    public void InitAsyncCommandProcessor()
    {
        _processorTask = Task.Run(CommandProcessor, _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        DisposeCore();

        if (_processorTask != null)
        {
            await _processorTask.ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void DisposeCore()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cmdChannel.Writer.TryComplete();
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task<TResult> CommandWithResponse<TResult>(Func<TResult> cmd)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        Action cmdWrapper = () =>
        {
            try
            {
                TResult result = cmd();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        };

        if (_cmdChannel.Writer.TryWrite(cmdWrapper) == false)
        {
            await _cmdChannel.Writer.WriteAsync(cmdWrapper);
        }
        
        return await tcs.Task;
    }

    private ValueTask Command(Action cmd)
    {
        if (_cmdChannel.Writer.TryWrite(cmd))
        {
            return ValueTask.CompletedTask;
        }

        return _cmdChannel.Writer.WriteAsync(cmd);
    }

    // fire-and-forget commands
    public ValueTask Add(TData data) => Command(() => AddInternal(data));
    public ValueTask Update(int index, TData data) => Command(() => UpdateInternal(index, data));
    public ValueTask Upsert(TData data) => Command(() => UpsertInternal(data)); 
    public ValueTask RemoveAt(int index) => Command(() => RemoveAtInternal(index)); 
    public ValueTask Clear() => Command(() => ClearInternal());

    // commands with response
    public Task<int> AddWithResponse(TData data) => CommandWithResponse(() => AddInternal(data));
    public Task<int> UpdateWithResponse(int index, TData data) => CommandWithResponse(() => UpdateInternal(index, data));
    public Task<int> UpsertWithResponse(TData data) => CommandWithResponse(() => UpsertInternal(data));
    public Task<int> RemoveAtWithResponse(int index) => CommandWithResponse(() => RemoveAtInternal(index));
    public Task<TData> GetWithResponse(int index) => CommandWithResponse(() => GetInternal(index));
    public Task<IReadOnlyList<TData>> CopyWithResponse() => CommandWithResponse(() => CopyInternal());
    public Task ClearWithResponse()
    {
        return CommandWithResponse(() =>
        {
            ClearInternal();
            return Task.CompletedTask;
        });
    }

    private async Task CommandProcessor()
    {
        try
        {
            await foreach (var cmd in _cmdChannel.Reader.ReadAllAsync(_cts.Token))
            {
                cmd();
            }
        }
        catch(OperationCanceledException)
        {
            // 정상 종료
        }
    }

    private int AddInternal(TData data)
    {
        _datas.Add(data);
        return _datas.Count - 1;
    }

    private int UpdateInternal(int index, TData data)
    {
        _datas[index] = data;
        return index;
    }

    private int RemoveAtInternal(int index)
    {
        _datas.RemoveAt(index);
        return index;
    }

    private TData GetInternal(int index)
    {
        return _datas[index];
    }

    private int UpsertInternal(TData data)
    {
        int index = _datas.FindIndex(d => d.Equals(data));
        if (index >= 0)
        {
            _datas[index] = data;
            return index;
        }
        else
        {
            _datas.Add(data);
            return _datas.Count - 1;
        }
    }

    private void ClearInternal()
    {
        _datas.Clear();
    }

    private IReadOnlyList<TData> CopyInternal()
    {
        return _datas.ToList();
    }
}