namespace ELImGui;

using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class ImRenderActionQueue<TContext>
{
    protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected readonly ConcurrentQueue<Action<TContext>> _queue = new();
    protected readonly ConcurrentQueue<Action<TContext>> _nextFrameQueue = new();
    protected int _renderThreadId = -1;
    protected bool _isInitialized = false;
    protected TContext _context = default!;

    public void Initialize(int renderThreadId, TContext context)
    {
        _renderThreadId = renderThreadId;
        _context = context;
        _isInitialized = true;
    }

    public bool IsRenderThread => Environment.CurrentManagedThreadId == _renderThreadId;
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Fire-and-forget
    /// </summary>
    public void Post(Action<TContext> action)
    {
        _queue.Enqueue(action);
    }

    public void NextFramePost(Action<TContext> action)
    {
        _nextFrameQueue.Enqueue(action);
    }

    /// <summary>
    /// await 가능. ImGui 렌더 스레드에서 호출시 await 하면 데드락
    /// </summary>
    public Task<TResult> Ask<TResult>(Func<TContext, TResult> func, CancellationToken cancellationToken = default)
    {
        return AskToQueue(_queue, func, cancellationToken);
    }

    public Task<TResult> NextFrameAsk<TResult>(Func<TContext, TResult> func, CancellationToken cancellationToken = default)
    {
        return AskToQueue(_nextFrameQueue, func, cancellationToken);
    }

    private Task<TResult> AskToQueue<TResult>(ConcurrentQueue<Action<TContext>> queue, Func<TContext, TResult> func, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        queue.Enqueue((context) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                tcs.TrySetResult(func(context));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{nameof(ImRenderActionQueue<TContext>)}.{nameof(AskToQueue)} exception : {ex}");
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    protected void Invoke(Action<TContext> action)
    {
        action(_context);
    }

    /// <summary>
    /// ImGui 렌더 스레드에서 호출되어야 함
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Work()
    {
        if (!IsRenderThread)
        {
            throw new InvalidOperationException($"{nameof(Work)} must be called from the ImGui render thread.");
        }

        while (_queue.TryDequeue(out var action))
        {
            try
            {
                Invoke(action);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{nameof(Work)} Action: {action}, exception : {ex}");
            }
        }

        while (_nextFrameQueue.TryDequeue(out var action))
        {
            _queue.Enqueue(action);
        }
    }
}