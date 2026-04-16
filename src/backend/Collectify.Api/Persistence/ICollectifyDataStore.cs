namespace Collectify.Api.Persistence;

public interface ICollectifyDataStore
{
    Task<CollectifyDataDocument> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(CollectifyDataDocument document, CancellationToken cancellationToken);
}
