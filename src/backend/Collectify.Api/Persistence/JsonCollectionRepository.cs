using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Persistence;

public sealed class JsonCollectionRepository(ICollectifyDataStore dataStore) : ICollectionRepository
{
    public async Task<IReadOnlyList<Collection>> ListAsync(CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);

        return document.Collections
            .OrderByDescending(collection => collection.UpdatedAt)
            .ToList();
    }

    public async Task<Collection?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        return document.Collections.FirstOrDefault(collection => collection.Id == id);
    }

    public async Task<Collection> AddAsync(Collection collection, CancellationToken cancellationToken)
    {
        return await dataStore.UpdateAsync(document =>
        {
            document.Collections.Add(collection);
            return DataStoreUpdate<Collection>.Changed(collection);
        }, cancellationToken);
    }

    public async Task<Collection?> SaveAsync(Collection collection, CancellationToken cancellationToken)
    {
        return await dataStore.UpdateAsync(document =>
        {
            var existing = document.Collections.FirstOrDefault(current => current.Id == collection.Id);

            if (existing is null)
            {
                return DataStoreUpdate<Collection?>.Unchanged(null);
            }

            var index = document.Collections.IndexOf(existing);
            document.Collections[index] = collection;

            return DataStoreUpdate<Collection?>.Changed(collection);
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dataStore.UpdateAsync(document =>
        {
            var deleted = document.Collections.RemoveAll(collection => collection.Id == id) > 0;
            return deleted
                ? DataStoreUpdate<bool>.Changed(true)
                : DataStoreUpdate<bool>.Unchanged(false);
        }, cancellationToken);
    }

    public async Task<TResult?> UpdateAsync<TResult>(
        Guid id,
        Func<Collection, CollectionUpdate<TResult>> update,
        CancellationToken cancellationToken)
    {
        return await dataStore.UpdateAsync(document =>
        {
            var collection = document.Collections.FirstOrDefault(current => current.Id == id);

            if (collection is null)
            {
                return DataStoreUpdate<TResult?>.Unchanged(default);
            }

            var result = update(collection);
            return result.HasChanges
                ? DataStoreUpdate<TResult?>.Changed(result.Value)
                : DataStoreUpdate<TResult?>.Unchanged(result.Value);
        }, cancellationToken);
    }

    public async Task<Item?> AddItemAsync(Guid collectionId, Item item, CancellationToken cancellationToken)
    {
        return await dataStore.UpdateAsync(document =>
        {
            var collection = document.Collections.FirstOrDefault(current => current.Id == collectionId);

            if (collection is null)
            {
                return DataStoreUpdate<Item?>.Unchanged(null);
            }

            item.CollectionId = collectionId;
            collection.Items.Add(item);
            collection.UpdatedAt = item.UpdatedAt;

            return DataStoreUpdate<Item?>.Changed(item);
        }, cancellationToken);
    }

    public async Task<bool> DeleteItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken)
    {
        return await dataStore.UpdateAsync(document =>
        {
            var collection = document.Collections.FirstOrDefault(current => current.Id == collectionId);

            if (collection is null)
            {
                return DataStoreUpdate<bool>.Unchanged(false);
            }

            var deleted = collection.Items.RemoveAll(item => item.Id == itemId) > 0;

            if (!deleted)
            {
                return DataStoreUpdate<bool>.Unchanged(false);
            }

            collection.UpdatedAt = DateTimeOffset.UtcNow;
            return DataStoreUpdate<bool>.Changed(true);
        }, cancellationToken);
    }
}
