namespace Collectify.Api.Modules.ExternalMetadata;

public interface IExternalMetadataProvider
{
    string ProviderId { get; }
    string SupportedKind { get; }
    Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken);
}
