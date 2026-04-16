using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Persistence;

public sealed class JsonCollectionCategoryRepository(ICollectifyDataStore dataStore) : ICollectionCategoryRepository
{
    public async Task<IReadOnlyList<CollectionCategory>> ListAsync(CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);

        return document.Categories
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToList();
    }

    public async Task<CollectionCategory?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        return document.Categories.FirstOrDefault(category => category.Id == id);
    }

    public async Task<CollectionCategory> SaveAsync(CollectionCategory category, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var existing = document.Categories.FirstOrDefault(current => current.Id == category.Id);

        category.Id = category.Id == Guid.Empty ? Guid.NewGuid() : category.Id;
        category.CreatedAt = existing?.CreatedAt ?? (category.CreatedAt == default ? now : category.CreatedAt);
        category.UpdatedAt = now;

        if (existing is null)
        {
            document.Categories.Add(category);
        }
        else
        {
            var index = document.Categories.IndexOf(existing);
            document.Categories[index] = category;
        }

        await dataStore.SaveAsync(document, cancellationToken);
        return category;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var deleted = document.Categories.RemoveAll(category => category.Id == id) > 0;

        if (deleted)
        {
            foreach (var collection in document.Collections.Where(collection => collection.CategoryId == id))
            {
                collection.CategoryId = null;
                collection.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await dataStore.SaveAsync(document, cancellationToken);
        }

        return deleted;
    }
}
