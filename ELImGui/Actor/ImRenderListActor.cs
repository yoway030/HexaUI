namespace ELImGui.Actor;

using System;
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

    public class PostAdapter
    {
        private readonly ImRenderListActor<TData> _actor;

        public PostAdapter(ImRenderListActor<TData> actor)
        {
            _actor = actor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPost(in TData item) => _actor.Post(new Message(MessageType.Add, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemovePost(in TData item) => _actor.Post(new Message(MessageType.Remove, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtPost(int index) => _actor.Post(new Message(MessageType.Remove, default, index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePost(in TData item) => _actor.Post(new Message(MessageType.Update, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAtPost(int index, in TData item) => _actor.Post(new Message(MessageType.Update, item, index));

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
            Func<DirectAdapter, TResult> func,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return _actor.Ask(() =>
            {
                var innderAdapter = _actor.GetDirectAdapter(memberName, filePath, lineNumber);
                return func(innderAdapter);
            });
        }
    }

    public class DirectAdapter
    {
        private readonly ImRenderListActor<TData> _actor;

        public DirectAdapter(ImRenderListActor<TData> actor)
        {
            _actor = actor;
        }

        public List<TData> Items => _actor._items;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDirect(in TData item) => _actor.HandleMessage(new Message(MessageType.Add, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveDirect(in TData item) => _actor.HandleMessage(new Message(MessageType.Remove, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtDirect(int index) => _actor.HandleMessage(new Message(MessageType.Remove, item: default, index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateDirect(in TData item) => _actor.HandleMessage(new Message(MessageType.Update, item));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAtDirect(in TData item, int index) => _actor.HandleMessage(new Message(MessageType.Update, item, index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDirect() => _actor.HandleMessage(new Message(MessageType.Clear));

    }

    private List<TData> _items = new();
    private PostAdapter? _postAdapter;
    private DirectAdapter? _directAdapter;

    public event InAction<TData>? OnAdded;
    public event InAction<TData>? OnRemoved;
    public event InAction<TData>? OnUpdated;
    public event Action? OnCleared;

    private void AddInternal(in TData item)
    {
        _items.Add(item);
        OnAdded?.Invoke(item);
    }

    private bool RemoveInternal(in TData item)
    {
        if (_items.Remove(item) == false)
        {
            return false;
        }

        OnRemoved?.Invoke(item);
        return true;
    }

    private bool RemoveAtInternal(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        var item = _items[index];
        _items.RemoveAt(index);
        OnRemoved?.Invoke(item);
        return true;
    }

    private bool UpdateInternal(in TData item)
    {
        int index = _items.IndexOf(item);
        return UpdateAtInternal(item, index);
    }

    private bool UpdateAtInternal(in TData item, int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        _items[index] = item;
        OnUpdated?.Invoke(_items[index]);

        return true;
    }

    private void ClearInternal()
    {
        _items.Clear();
        OnCleared?.Invoke();
    }

    protected override void HandleMessage(in Message message)
    {
        switch (message.Type)
        {
            case MessageType.Add:
                AddInternal(message.Item!);
                break;

            case MessageType.Remove:
                if (message.Index.HasValue)
                {
                    RemoveAtInternal(message.Index.Value);
                }
                else
                {
                    RemoveInternal(message.Item!);
                }

                break;

            case MessageType.Update:
                if (message.Index.HasValue)
                {
                    UpdateAtInternal(message.Item!, message.Index.Value);
                }
                else
                {
                    UpdateInternal(message.Item!);
                }

                break;

            case MessageType.Clear:
                ClearInternal();
                break;

            default:
                throw new Exception($"{nameof(ImRenderListActor<TData>)} get unknown message type:{message.Type}");
        }
    }

    public PostAdapter GetPostAdapter(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        CheckPostableThread(memberName, filePath, lineNumber);
        return _postAdapter ??= new PostAdapter(this);
    }

    public DirectAdapter GetDirectAdapter(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        CheckDirectableThread(memberName, filePath, lineNumber);
        return _directAdapter ??= new DirectAdapter(this);
    }
}