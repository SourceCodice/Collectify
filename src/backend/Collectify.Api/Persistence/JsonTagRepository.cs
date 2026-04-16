using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Persistence;

public sealed class JsonTagRepository(ICollectifyDataStore dataStore) : ITagRepository
{
    public async Task<IReadOnlyList<Tag>> ListAsync(CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);

        return document.Tags
            .OrderBy(tag => tag.Name)
            .ToList();
    }

    public async Task<Tag?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        return document.Tags.FirstOrDefault(tag => tag.Id == id);
    }

    public async Task<Tag> SaveAsync(Tag tag, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var existing = document.Tags.FirstOrDefault(current => current.Id == tag.Id);

        tag.Id = tag.Id == Guid.Empty ? Guid.NewGuid() : tag.Id;
        tag.CreatedAt = existing?.CreatedAt ?? (tag.CreatedAt == default ? now : tag.CreatedAt);
        tag.UpdatedAt = now;

        if (existing is null)
        {
            document.Tags.Add(tag);
        }
        else
        {
            var index = document.Tags.IndexOf(existing);
            document.Tags[index] = tag;
        }

        await dataStore.SaveAsync(document, cancellationToken);
        return tag;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var deleted = document.Tags.RemoveAll(tag => tag.Id == id) > 0;

        if (deleted)
        {
            foreach (var item in document.Collections.SelectMany(collection => collection.Items))
            {
                item.TagIds.RemoveAll(tagId => tagId == id);
                item.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await dataStore.SaveAsync(document, cancellationToken);
        }

        return deleted;
    }
}
