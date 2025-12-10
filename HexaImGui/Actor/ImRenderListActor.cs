using System.Runtime.CompilerServices;

namespace ELImGui.Actor;

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
            => new Message(MessageType.Ask, askPayload: new ActorAskPayload<TResult>(func, tcs));

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

        public void AddPost(in TData item) => _actor.Post(new Message(MessageType.Add, item));
        public void RemovePost(in TData item) => _actor.Post(new Message(MessageType.Remove, item));
        public void UpdatePost(in TData item) => _actor.Post(new Message(MessageType.Update, item));
        public void ClearPost() => _actor.Post(new Message(MessageType.Clear));
        public Task<List<TData>> SnapshotAsk()
        {
            return _actor.Ask(() =>
            {
                return new List<TData>(_actor._items);
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
    }

    private List<TData> _items = new();

    protected override void HandleMessage(in Message message)
    {
        switch (message.Type)
        {
            case MessageType.Add:
                _items.Add(message.Item!);
                break;

            case MessageType.Remove:
                _items.Remove(message.Item!);
                break;

            case MessageType.Update:
                {
                    var index = _items.IndexOf(message.Item!);
                    if (index >= 0)
                    {
                        _items[index] = message.Item!;
                    }
                }
                break;

            case MessageType.Clear:
                _items.Clear();
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
        return new OuterAdapter(this);
    }

    public InnerAdapter GetInnerAdapter(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        CheckInnerRenderThread(memberName, filePath, lineNumber);
        return new InnerAdapter(this);
    }
}