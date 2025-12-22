namespace ELImGui.Actor;

using System.Runtime.CompilerServices;

public class ImRenderListActor<TData> : ImRenderBaseActor<ImRenderListActor<TData>.Message>
{
    public enum MessageType
    {
        Add,
        Remove,
        Update,
        Clear,
        Ask,
    }

    public readonly record struct Message : IRenderActorMessage<Message>
    {
        public static Message CreateAskMessage<TResult>(Func<TResult> func, TaskCompletionSource<TResult> tcs)
            => new(MessageType.Ask, askPayload: new ActorAskPayload<TResult>(func, tcs));

        public Message(MessageType type, TData? item = default, int? index = null, IActorAskPayLoad? askPayload = default)
        {
            Type = type;
            Item = item;
            Index = index;
            AskPayload = askPayload;
        }

        public MessageType Type { get; }
        public TData? Item { get; }
        public int? Index { get; }
        public IActorAskPayLoad? AskPayload { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAskMessage()
        {
            return AskPayload != null;
        }

        public void InvokeAsk()
        {
            AskPayload?.InvokeAsk();
        }
    }

    public class OuterAdapter
    {
        private readonly ImRenderListActor<TData> _actor;

        public OuterAdapter(ImRenderListActor<TData> actor)
        {
            _actor = actor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPost(in TData item) => _actor.Post(new Message(MessageType.Add, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemovePost(in TData item) => _actor.Post(new Message(MessageType.Remove, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePost(in TData item) => _actor.Post(new Message(MessageType.Update, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearPost() => _actor.Post(new Message(MessageType.Clear));

        public Task<List<TData>> SnapshotAsk()
        {
            return _actor.Ask(() =>
            {
                return new List<TData>(_actor._items);
            });
        }

        public Task<TResult> Ask<TResult>(
            Func<InnerAdapter, TResult> func,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return _actor.Ask(() =>
            {
                var innderAdapter = _actor.GetInnerAdapter(memberName, filePath, lineNumber);
                return func(innderAdapter);
            });
        }
    }

    public class InnerAdapter
    {
        private readonly ImRenderListActor<TData> _actor;

        public InnerAdapter(ImRenderListActor<TData> actor)
        {
            _actor = actor;
        }

        public List<TData> Items => _actor._items;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in TData item) => Items.Add(item);
    }

    private List<TData> _items = new();
    private OuterAdapter? _outer;
    private InnerAdapter? _inner;

    public event InAction<Message>? OnAdded;
    public event InAction<Message>? OnRemoved;
    public event InAction<Message>? OnUpdated;
    public event InAction<Message>? OnCleared;

    protected override void HandleMessage(in Message message)
    {
        switch (message.Type)
        {
            case MessageType.Add:
                _items.Add(message.Item!);
                OnAdded?.Invoke(message);
                break;

            case MessageType.Remove:
                if (_items.Remove(message.Item!) == true)
                {
                    OnRemoved?.Invoke(message);
                }

                break;

            case MessageType.Update:
            {
                int index = _items.IndexOf(message.Item!);
                if (index >= 0)
                {
                    _items[index] = message.Item!;
                    OnUpdated?.Invoke(message);
                }
            }

            break;

            case MessageType.Clear:
                _items.Clear();
                OnCleared?.Invoke(message);
                break;

            default:
                throw new Exception($"{nameof(ImRenderListActor<TData>)} get unknown message type:{message.Type}");
        }
    }

    public OuterAdapter GetOuterAdapter(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        CheckOuterRenderThread(memberName, filePath, lineNumber);
        return _outer ??= new OuterAdapter(this);
    }

    public InnerAdapter GetInnerAdapter(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        CheckInnerRenderThread(memberName, filePath, lineNumber);
        return _inner ??= new InnerAdapter(this);
    }
}