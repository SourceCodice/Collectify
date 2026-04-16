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

    public async Task<Collection> CreateAsync(CreateCollectionRequest request, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Description = Normalize(request.Description),
            CreatedAt = now,
            UpdatedAt = now
        };

        document.Collections.Add(collection);
        await dataStore.SaveAsync(document, cancellationToken);

        return collection;
    }

    public async Task<Collection?> UpdateAsync(Guid id, UpdateCollectionRequest request, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var collection = document.Collections.FirstOrDefault(current => current.Id == id);

        if (collection is null)
        {
            return null;
        }

        collection.Name = request.Name.Trim();
        collection.Type = request.Type.Trim();
        collection.Description = Normalize(request.Description);
        collection.UpdatedAt = DateTimeOffset.UtcNow;

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

    public async Task<Item?> AddItemAsync(Guid collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var collection = document.Collections.FirstOrDefault(current => current.Id == collectionId);

        if (collection is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var item = new Item
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            Notes = Normalize(request.Notes),
            Condition = string.IsNullOrWhiteSpace(request.Condition) ? "Non specificato" : request.Condition.Trim(),
            AcquiredAt = request.AcquiredAt,
            Attributes = BuildAttributes(request.Attributes, now),
            TagIds = request.TagIds?.Distinct().ToList() ?? [],
            ExternalReferences = BuildExternalReferences(request.ExternalReferences, now),
            CreatedAt = now,
            UpdatedAt = now
        };

        collection.Items.Add(item);
        collection.UpdatedAt = now;

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

    private static List<ItemAttribute> BuildAttributes(IReadOnlyList<ItemAttributeRequest>? requests, DateTimeOffset now)
    {
        if (requests is null)
        {
            return [];
        }

        return requests
            .Where(request => !string.IsNullOrWhiteSpace(request.Key))
            .Select(request => new ItemAttribute
            {
                Id = Guid.NewGuid(),
                Key = request.Key.Trim(),
                Label = string.IsNullOrWhiteSpace(request.Label) ? request.Key.Trim() : request.Label.Trim(),
                Value = request.Value.Trim(),
                ValueType = string.IsNullOrWhiteSpace(request.ValueType) ? "Text" : request.ValueType.Trim(),
                Unit = Normalize(request.Unit),
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();
    }

    private static List<ExternalReference> BuildExternalReferences(IReadOnlyList<ExternalReferenceRequest>? requests, DateTimeOffset now)
    {
        if (requests is null)
        {
            return [];
        }

        return requests
            .Where(request => !string.IsNullOrWhiteSpace(request.Provider))
            .Select(request => new ExternalReference
            {
                Id = Guid.NewGuid(),
                Provider = request.Provider.Trim(),
                ExternalId = Normalize(request.ExternalId),
                Url = Normalize(request.Url),
                Metadata = request.Metadata ?? [],
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
