namespace AsyncDataTable;

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
    private readonly CancellationTokenSource _cts = new();
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
    protected CancellationToken CancellationToken => _cts.Token;

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
            while (await Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
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

    /// <summary>
    /// 파생 클래스의 Command 처리 로직
    /// </summary>
    protected abstract void HandleCommand(in TCommand cmd);

    /// <summary>
    /// 외부에서 수동으로 한 번씩 폴링할 때 사용.
    /// </summary>
    public void TryWork()
    {
        while (Reader.TryRead(out var cmd))
        {
            _cts.Token.ThrowIfCancellationRequested();
            HandleCommand(in cmd);
        }
    }

    protected ValueTask SendCommand(TCommand cmd)
        => Writer.WriteAsync(cmd);

    public async ValueTask DisposeAsync()
    {
        Writer.TryComplete();

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

    protected string DebugStringImpl(Func<TCollection, string> toString)
        => toString(Items);
}

/// <summary>
/// 컬렉션에 대해 동작하는 확장 커맨드 인터페이스.
/// </summary>
public interface IExtensionCommand<TCollection>
{
    void Execute(TCollection collection);
}

/// <summary>
/// 컬렉션을 읽어서 TResult 를 돌려주는 확장 커맨드 구현.
/// List/Dictionary 양쪽에서 재사용.
/// </summary>
public sealed class AskExtensionCommand<TCollection, TResult> : IExtensionCommand<TCollection>
{
    public AskExtensionCommand(
        Func<TCollection, TResult> func,
        TaskCompletionSource<TResult> tcs)
    {
        Func = func;
        Tcs = tcs;
    }

    public Func<TCollection, TResult> Func { get; }
    public TaskCompletionSource<TResult> Tcs { get; }

    public void Execute(TCollection collection)
    {
        try
        {
            var result = Func(collection);
            Tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            Tcs.SetException(ex);
        }
    }
}
