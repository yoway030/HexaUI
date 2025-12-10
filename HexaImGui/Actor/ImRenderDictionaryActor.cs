using System.Runtime.CompilerServices;

namespace ELImGui.Actor;

public class ImRenderDictionaryActor<TKey, TValue> : ImRenderBaseActor<ImRenderDictionaryActor<TKey, TValue>.Message>
    where TKey : notnull
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

        public Message(MessageType type, TKey? key = default, TValue? value = default, IActorAskPayLoad? askPayload = default)
        {
            Type = type;
            Key = key;
            Value = value;
            AskPayload = askPayload;
        }

        public MessageType Type { get; }
        public TKey? Key { get; }
        public TValue? Value { get; }
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
        private readonly ImRenderDictionaryActor<TKey, TValue> _actor;

        public OuterAdapter(ImRenderDictionaryActor<TKey, TValue> actor)
        {
            _actor = actor;
        }

        public void AddPost(in TKey key, in TValue value) => _actor.Post(new Message(MessageType.Add, key, value));
        public void RemovePost(in TKey key) => _actor.Post(new Message(MessageType.Remove, key));
        public void UpdatePost(in TKey key, in TValue value) => _actor.Post(new Message(MessageType.Update, key, value));
        public void ClearPost() => _actor.Post(new Message(MessageType.Clear));
        public Task<Dictionary<TKey, TValue>> SnapshotAsk()
        {
            return _actor.Ask(() =>
            {
                return new Dictionary<TKey, TValue>(_actor._items);
            });
        }
    }

    public class InnerAdapter
    {
        private readonly ImRenderDictionaryActor<TKey, TValue> _actor;

        public InnerAdapter(ImRenderDictionaryActor<TKey, TValue> actor)
        {
            _actor = actor;
        }

        public Dictionary<TKey, TValue> Items => _actor._items;
    }

    private Dictionary<TKey, TValue> _items = new();

    protected override void HandleMessage(in Message message)
    {
        switch (message.Type)
        {
            case MessageType.Add:
                _items.Add(message.Key!, message.Value!);
                break;

            case MessageType.Remove:
                _items.Remove(message.Key!);
                break;

            case MessageType.Update:
                _items[message.Key!] = message.Value!;
                break;

            case MessageType.Clear:
                _items.Clear();
                break;

            default:
                throw new Exception($"{nameof(ImRenderDictionaryActor<TKey, TValue>)} get unknown message type:{message.Type}");
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