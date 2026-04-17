namespace Collectify.Api.Modules.Collections;

public interface ICollectionRepository
{
    Task<IReadOnlyList<Collection>> ListAsync(CancellationToken cancellationToken);
    Task<Collection?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Collection> AddAsync(Collection collection, CancellationToken cancellationToken);
    Task<Collection?> SaveAsync(Collection collection, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<TResult?> UpdateAsync<TResult>(
        Guid id,
        Func<Collection, CollectionUpdate<TResult>> update,
        CancellationToken cancellationToken);
    Task<Item?> AddItemAsync(Guid collectionId, Item item, CancellationToken cancellationToken);
    Task<bool> DeleteItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken);
}

public sealed record CollectionUpdate<TResult>(TResult? Value, bool HasChanges)
{
    public static CollectionUpdate<TResult> Changed(TResult? value)
    {
        return new CollectionUpdate<TResult>(value, true);
    }

    public static CollectionUpdate<TResult> Unchanged(TResult? value = default)
    {
        return new CollectionUpdate<TResult>(value, false);
    }
}
