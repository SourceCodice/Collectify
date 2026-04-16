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
        var document = await dataStore.LoadAsync(cancellationToken);

        document.Collections.Add(collection);
        await dataStore.SaveAsync(document, cancellationToken);

        return collection;
    }

    public async Task<Collection?> SaveAsync(Collection collection, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var existing = document.Collections.FirstOrDefault(current => current.Id == collection.Id);

        if (existing is null)
        {
            return null;
        }

        var index = document.Collections.IndexOf(existing);
        document.Collections[index] = collection;

        await dataStore.SaveAsync(document, cancellationToken);
        return collection;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var deleted = document.Collections.RemoveAll(collection => collection.Id == id) > 0;

        if (deleted)
        {
            await dataStore.SaveAsync(document, cancellationToken);
        }

        return deleted;
    }

    public async Task<Item?> AddItemAsync(Guid collectionId, Item item, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var collection = document.Collections.FirstOrDefault(current => current.Id == collectionId);

        if (collection is null)
        {
            return null;
        }

        item.CollectionId = collectionId;
        collection.Items.Add(item);
        collection.UpdatedAt = item.UpdatedAt;

        await dataStore.SaveAsync(document, cancellationToken);
        return item;
    }

    public async Task<bool> DeleteItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var collection = document.Collections.FirstOrDefault(current => current.Id == collectionId);

        if (collection is null)
        {
            return false;
        }

        var deleted = collection.Items.RemoveAll(item => item.Id == itemId) > 0;

        if (deleted)
        {
            collection.UpdatedAt = DateTimeOffset.UtcNow;
            await dataStore.SaveAsync(document, cancellationToken);
        }

        return deleted;
    }

}
