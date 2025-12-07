namespace ELImGui;

using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class ImRenderActionQueue
{
    protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected readonly ConcurrentQueue<Action<ImInternalContext>> _queue = new();
    protected readonly ConcurrentQueue<Action<ImInternalContext>> _nextFrameQueue = new();
    protected int _renderThreadId = -1;
    protected ImInternalContext _imInternalContext = null!;

    public void Initialize(int renderThreadId, ImInternalContext imInternalContext)
    {
        _renderThreadId = renderThreadId;
        _imInternalContext = imInternalContext;
    }

    public bool IsRenderThread => Environment.CurrentManagedThreadId == _renderThreadId;

    /// <summary>
    /// Fire-and-forget
    /// </summary>
    public void Post(Action<ImInternalContext> item)
    {
        _queue.Enqueue(item);
    }

    public void NextFramePost(Action<ImInternalContext> item)
    {
        _nextFrameQueue.Enqueue(item);
    }

    public Task<TResult> Ask<TResult>(Func<ImInternalContext, TResult> func, CancellationToken cancellationToken = default)
    {
        return AskToQueue(_queue, func, cancellationToken);
    }

    public Task<TResult> NextFrameAsk<TResult>(Func<ImInternalContext, TResult> func, CancellationToken cancellationToken = default)
    {
        return AskToQueue(_nextFrameQueue, func, cancellationToken);
    }

    private Task<TResult> AskToQueue<TResult>(ConcurrentQueue<Action<ImInternalContext>> queue, Func<ImInternalContext, TResult> func, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ctr = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled(cancellationToken);
        });

        queue.Enqueue((imInternalContext) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                ctr.Dispose();
                return;
            }

            try
            {
                tcs.TrySetResult(func(imInternalContext));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{nameof(ImRenderActionQueue)}.{nameof(AskToQueue)} exception : {ex}");
                tcs.TrySetException(ex);
            }

            ctr.Dispose();
        });

        return tcs.Task;
    }

    protected void Invoke(Action<ImInternalContext> action)
    {
        action(_imInternalContext);
    }

    /// <summary>
    /// ImGui 렌더 스레드에서 호출되어야 함
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Flush()
    {
        if (!IsRenderThread)
        {
            throw new InvalidOperationException($"{nameof(Flush)} must be called from the ImGui render thread.");
        }

        while (_queue.TryDequeue(out var item))
        {
            try 
            {
                Invoke(item);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{nameof(Flush)} item: {item}, exception : {ex}");
            }
        }

        while (_nextFrameQueue.TryDequeue(out var work))
        {
            _queue.Enqueue(work);
        }
    }
}