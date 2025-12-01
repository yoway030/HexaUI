namespace ELImGui.Core;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// 컬렉션 단위로 동작하는 Actor 공통 베이스.
/// - Channel 루프
/// - CancellationTokenSource
/// - InitAsyncTask / DisposeAsync / TryWork
/// </summary>
public abstract class CollectionActorBase<TCollection, TCommand> : IAsyncDisposable
{
    private readonly Channel<TCommand> _channel;
    private readonly CancellationTokenSource _loopCts = new();
    private Task _loopTask = Task.CompletedTask;

    protected CollectionActorBase(bool useTask = true)
    {
        _channel = Channel.CreateUnbounded<TCommand>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        if (useTask)
        {
            InitAsyncTask();
        }
    }

    // 파생 클래스에서 컬렉션 자체를 가지고 있도록 강제
    protected abstract TCollection Items { get; }

    protected ChannelWriter<TCommand> Writer => _channel.Writer;
    protected ChannelReader<TCommand> Reader => _channel.Reader;
    protected CancellationToken CancellationToken => _loopCts.Token;

    // read thread나 task에서만 접근할 때 사용하는 복사본
    public TCollection ItemsCopy => Items;

    /// <summary>
    /// 채널 루프를 Task로 실행
    /// </summary>
    public void InitAsyncTask()
    {
        _loopTask = Task.Run(WorkAsync);
    }

    private async Task WorkAsync()
    {
        try
        {
            while (await Reader.WaitToReadAsync(_loopCts.Token).ConfigureAwait(false))
            {
                await TryWork();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
        }
    }

    /// <summary>
    /// 파생 클래스의 Command 처리 로직
    /// </summary>
    protected abstract ValueTask HandleCommand(in TCommand cmd);

    /// <summary>
    /// 외부에서 수동으로 한 번씩 폴링할 때 사용.
    /// </summary>
    public async ValueTask<int> TryWork()
    {
        int readCount = 0;

        while (Reader.TryRead(out var cmd))
        {
            readCount++;
            _loopCts.Token.ThrowIfCancellationRequested();
            var task = HandleCommand(in cmd);

            if (task.IsCompletedSuccessfully)
            {
                continue;
            }

            await AwaitSlow(task).ConfigureAwait(false);
        }

        return readCount;

        static async ValueTask AwaitSlow(ValueTask task)
        {
            await task.ConfigureAwait(false);
        }
    }

    protected void SendCommand(in TCommand cmd)
        => _ = Writer.WriteAsync(cmd);

    public async ValueTask DisposeAsync()
    {
        Writer.TryComplete();
        _loopCts.Cancel();

        try
        {
            await _loopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _loopCts.Dispose();
    }

    protected string DebugStringImpl(Func<TCollection, string> toString)
        => toString(Items);
}

/// <summary>
/// 컬렉션에 대해 동작하는 확장 커맨드 인터페이스.
/// </summary>
public interface IExtensionCommand<TCollection>
{
    ValueTask ExecuteAsync(TCollection collection);
}

public sealed class AsyncCommand<TCollection, TResult> : IExtensionCommand<TCollection>
{
    private Func<TCollection, ValueTask<TResult>> _asyncFunc { get; }
    private TaskCompletionSource<TResult> _tcs { get; }

    public AsyncCommand(
        Func<TCollection, ValueTask<TResult>> asyncFunc,
        TaskCompletionSource<TResult> tcs)
    {
        _asyncFunc = asyncFunc;
        _tcs = tcs;
    }

    public async ValueTask ExecuteAsync(TCollection collection)
    {
        try
        {
            var result = await _asyncFunc(collection).ConfigureAwait(false);
            _tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }
}
