using System.Collections.Concurrent;

namespace Collectify.Api.Modules.Collections;

public sealed class InMemoryCollectionRepository : ICollectionRepository
{
    private readonly ConcurrentDictionary<Guid, Collection> _collections = new();

    public InMemoryCollectionRepository()
    {
        var now = DateTimeOffset.UtcNow;
        var seeds = new[]
        {
            new Collection(
                Guid.Parse("8a0bb4c6-26f7-4c7d-9cd5-5f25a9c75f01"),
                "Film preferiti",
                "Movies",
                "Blu-ray, DVD e film digitali da tenere d'occhio.",
                [
                    new CollectionItem(
                        Guid.Parse("c7e1b785-f255-468d-95e7-994a38f24b8f"),
                        "Blade Runner",
                        "Director's cut",
                        "Ottimo",
                        now,
                        now,
                        now)
                ],
                now,
                now),
            new Collection(
                Guid.Parse("2407d0c8-cbd3-41d8-bc1d-8213978b7e41"),
                "Piante di casa",
                "Plants",
                "Specie, cure e note sulle ultime annaffiature.",
                [],
                now,
                now)
        };

        foreach (var seed in seeds)
        {
            _collections.TryAdd(seed.Id, seed);
        }
    }

    public Task<IReadOnlyList<Collection>> ListAsync(CancellationToken cancellationToken)
    {
        var collections = _collections.Values
            .OrderByDescending(collection => collection.UpdatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Collection>>(collections);
    }

    public Task<Collection?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _collections.TryGetValue(id, out var collection);
        return Task.FromResult(collection);
    }

    public Task<Collection> CreateAsync(CreateCollectionRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var collection = new Collection(
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Type.Trim(),
            Normalize(request.Description),
            [],
            now,
            now);

        _collections[collection.Id] = collection;
        return Task.FromResult(collection);
    }

    public Task<Collection?> UpdateAsync(Guid id, UpdateCollectionRequest request, CancellationToken cancellationToken)
    {
        if (!_collections.TryGetValue(id, out var current))
        {
            return Task.FromResult<Collection?>(null);
        }

        var updated = current with
        {
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Description = Normalize(request.Description),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _collections[id] = updated;
        return Task.FromResult<Collection?>(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_collections.TryRemove(id, out _));
    }

    public Task<CollectionItem?> AddItemAsync(Guid collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken)
    {
        if (!_collections.TryGetValue(collectionId, out var collection))
        {
            return Task.FromResult<CollectionItem?>(null);
        }

        var now = DateTimeOffset.UtcNow;
        var item = new CollectionItem(
            Guid.NewGuid(),
            request.Title.Trim(),
            Normalize(request.Notes),
            string.IsNullOrWhiteSpace(request.Condition) ? "Non specificato" : request.Condition.Trim(),
            request.AcquiredAt ?? now,
            now,
            now);

        var updated = collection with
        {
            Items = collection.Items.Append(item).ToList(),
            UpdatedAt = now
        };

        _collections[collectionId] = updated;
        return Task.FromResult<CollectionItem?>(item);
    }

    public Task<bool> DeleteItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!_collections.TryGetValue(collectionId, out var collection))
        {
            return Task.FromResult(false);
        }

        var items = collection.Items
            .Where(item => item.Id != itemId)
            .ToList();

        if (items.Count == collection.Items.Count)
        {
            return Task.FromResult(false);
        }

        _collections[collectionId] = collection with
        {
            Items = items,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(true);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
