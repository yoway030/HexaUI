namespace ELImGui.Actor;

using NLog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public abstract class ImRenderBaseActor<TMessage>
    where TMessage : struct, IRenderActorMessage<TMessage>
{
    protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected readonly ConcurrentQueue<TMessage> _queue = new();
    protected readonly ConcurrentQueue<TMessage> _nextFrameQueue = new();
    protected int _renderThreadId = -1;
    protected bool _isInitialized = false;

    public void Initialize(int renderThreadId)
    {
        _renderThreadId = renderThreadId;
        _isInitialized = true;
    }

    public bool IsRenderThread => Environment.CurrentManagedThreadId == _renderThreadId;
    public bool IsInitialized => _isInitialized;

    [Conditional("DEBUG")]
    public void CheckDirectableThread(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsRenderThread)
        {
            throw new InvalidOperationException($"{nameof(CheckDirectableThread)} check failed" +
                $"thread={Environment.CurrentManagedThreadId} != {_renderThreadId}," +
                $"source={memberName}:{Path.GetFileName(filePath)}:{lineNumber}");
        }
    }

    [Conditional("DEBUG")]
    public void CheckPostableThread(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (IsRenderThread)
        {
            throw new InvalidOperationException($"{nameof(CheckPostableThread)} check failed" +
                $"thread={Environment.CurrentManagedThreadId} != {_renderThreadId}," +
                $"source={memberName}:{Path.GetFileName(filePath)}:{lineNumber}");
        }
    }

    /// <summary>
    /// Fire-and-forget
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Post(in TMessage message)
    {
        _queue.Enqueue(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void NextFramePost(in TMessage message)
    {
        _nextFrameQueue.Enqueue(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Task<TResult> Ask<TResult>(Func<TResult> func)
    {
        return AskToQueue(_queue, func);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Task<TResult> NextFrameAsk<TResult>(Func<TResult> func)
    {
        return AskToQueue(_nextFrameQueue, func);
    }

    private Task<TResult> AskToQueue<TResult>(ConcurrentQueue<TMessage> queue, Func<TResult> func)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Enqueue(TMessage.CreateAskMessage(func, tcs));
        return tcs.Task;
    }

    protected abstract void HandleMessage(in TMessage message);

    public void Work()
    {
        CheckDirectableThread();

        while (_queue.TryDequeue(out var message))
        {
            try
            {
                if (message.IsAskMessage())
                {
                    message.InvokeAsk();
                }
                else
                {
                    HandleMessage(message);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{nameof(Work)} Action: {message}, exception : {ex}");
            }
        }

        while (_nextFrameQueue.TryDequeue(out var message))
        {
            _queue.Enqueue(message);
        }
    }
}

public interface IRenderActorMessage<TSelf>
    where TSelf : struct, IRenderActorMessage<TSelf>
{
    public static abstract TSelf CreateAskMessage<TResult>(Func<TResult> func, TaskCompletionSource<TResult> tcs);

    public bool IsAskMessage();

    public void InvokeAsk();
}

public interface IActorAskPayLoad
{
    public void InvokeAsk();
}

public class ActorAskPayload<TResult> : IActorAskPayLoad
{
    public ActorAskPayload(Func<TResult> func, TaskCompletionSource<TResult> tcs)
    {
        _func = func;
        _tcs = tcs;
    }

    private Func<TResult> _func { get; }
    private TaskCompletionSource<TResult> _tcs { get; }

    public void InvokeAsk()
    {
        try
        {
            var result = _func();
            _tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }
}