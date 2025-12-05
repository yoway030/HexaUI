namespace ELImGui.Core;

using System.Collections.Immutable;

public class CollectionActorSnapshotCache<TCollectionActor, TCollection, TData, TCommand>
    where TCollectionActor : CollectionActorBase<TCollection, TData, TCommand>
    where TCollection : IEnumerable<TData>
    where TCommand : struct
{
    private int _modifyCount = 0;
    private ImmutableArray<TData> _snapshot = ImmutableArray<TData>.Empty;

    public async ValueTask<ImmutableArray<TData>> GetSnapshotCache(TCollectionActor actor)
    {
        if (actor.IsModified(_modifyCount, out _modifyCount) == true)
        {
            var snapshotTask = await actor.SnapshotAsync();
            _snapshot = snapshotTask.ToImmutableArray();
        }

        return _snapshot;
    }
}
