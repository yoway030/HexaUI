namespace ELImGui;

using NLog;
using System.Collections.Concurrent;

public abstract class ImRenderDataStructureActorBase<TMessage>
    where TMessage : struct, IImRenderDataStructureActorMessage<TMessage>
{
    protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected readonly ConcurrentQueue<TMessage> _queue = new();
    protected readonly ConcurrentQueue<TMessage> _nextFrameQueue = new();
    protected int _renderThreadId = -1;

    public void Initialize(int renderThreadId)
    {
        _renderThreadId = renderThreadId;
    }

    public bool IsRenderThread => Environment.CurrentManagedThreadId == _renderThreadId;

    /// <summary>
    /// Fire-and-forget
    /// </summary>
    public void Post(in TMessage message)
    {
        _queue.Enqueue(message);
    }

    public void NextFramePost(in TMessage message)
    {
        _nextFrameQueue.Enqueue(message);
    }

    public Task<TResult> Ask<TResult>(Func<TMessage, TResult> func, CancellationToken cancellationToken = default)
    {
        return AskToQueue(_queue, func, cancellationToken);
    }

    public Task<TResult> NextFrameAsk<TResult>(Func<TMessage, TResult> func, CancellationToken cancellationToken = default)
    {
        return AskToQueue(_nextFrameQueue, func, cancellationToken);
    }

    private Task<TResult> AskToQueue<TResult>(ConcurrentQueue<TMessage> queue, Func<TMessage, TResult> func, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        //queue.Enqueue((context) =>
        //{
        //    if (cancellationToken.IsCancellationRequested)
        //    {
        //        tcs.TrySetCanceled(cancellationToken);
        //        return;
        //    }

        //    try
        //    {
        //        tcs.TrySetResult(func(context));
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex, $"{nameof(ImRenderDataStructureActorBase<TMessage>)}.{nameof(AskToQueue)} exception : {ex}");
        //        tcs.TrySetException(ex);
        //    }
        //});

        return tcs.Task;
    }

    protected virtual void HandleMessage(TMessage message)
    {
    }

    public void Work()
    {
        if (!IsRenderThread)
        {
            throw new InvalidOperationException($"{nameof(Work)} must be called from the ImGui render thread.");
        }

        while (_queue.TryDequeue(out var message))
        {
            try
            {
                HandleMessage(message);
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

public interface IImRenderDataStructureActorMessage<TSelf>
    where TSelf : struct, IImRenderDataStructureActorMessage<TSelf>
{
}

public readonly record struct ListActorMessage<TData> : IImRenderDataStructureActorMessage<ListActorMessage<TData>>
{
    public enum MessageType
    {
        Add,
        Ask
    }

    public readonly record struct AskPayload<TResult>(
        Func<List<TData>, TResult> Func,
        TaskCompletionSource<TResult> Tcs);

    public ListActorMessage(MessageType type, TData? item, int? index = null)
    {
        Type = type;
        Item = item;
        Index = index;
    }

    public MessageType Type { get; }
    public TData? Item { get; }
    public int? Index { get; }
}


public class ImRenderListActor<TData> : ImRenderDataStructureActorBase<ListActorMessage<TData>>
{
    public ImRenderListActor()
    {
    }

    private readonly List<TData> _items = new();

    protected override void HandleMessage(ListActorMessage<TData> message)
    {

    }
}