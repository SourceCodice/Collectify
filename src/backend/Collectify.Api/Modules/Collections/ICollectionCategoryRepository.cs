namespace Collectify.Api.Modules.Collections;

public interface ICollectionCategoryRepository
{
    Task<IReadOnlyList<CollectionCategory>> ListAsync(CancellationToken cancellationToken);
    Task<CollectionCategory?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CollectionCategory> SaveAsync(CollectionCategory category, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
