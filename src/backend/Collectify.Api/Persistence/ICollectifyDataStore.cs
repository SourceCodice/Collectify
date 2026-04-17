namespace Collectify.Api.Persistence;

public interface ICollectifyDataStore
{
    Task<CollectifyDataDocument> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(CollectifyDataDocument document, CancellationToken cancellationToken);
    Task<TResult> UpdateAsync<TResult>(
        Func<CollectifyDataDocument, DataStoreUpdate<TResult>> update,
        CancellationToken cancellationToken);
}

public sealed record DataStoreUpdate<TResult>(TResult Value, bool HasChanges)
{
    public static DataStoreUpdate<TResult> Changed(TResult value)
    {
        return new DataStoreUpdate<TResult>(value, true);
    }

    public static DataStoreUpdate<TResult> Unchanged(TResult value)
    {
        return new DataStoreUpdate<TResult>(value, false);
    }
}
