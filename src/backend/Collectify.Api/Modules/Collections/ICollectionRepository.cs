namespace Collectify.Api.Modules.Collections;

public interface ICollectionRepository
{
    Task<IReadOnlyList<Collection>> ListAsync(CancellationToken cancellationToken);
    Task<Collection?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Collection> CreateAsync(CreateCollectionRequest request, CancellationToken cancellationToken);
    Task<Collection?> UpdateAsync(Guid id, UpdateCollectionRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<CollectionItem?> AddItemAsync(Guid collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken);
}
