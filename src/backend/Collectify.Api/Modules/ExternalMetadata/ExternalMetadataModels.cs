namespace Collectify.Api.Modules.ExternalMetadata;

public static class ExternalMetadataKinds
{
    public const string Movie = "movie";
    public const string TvShow = "show";
    public const string Game = "game";
    public const string Album = "album";
    public const string Book = "book";
    public const string Manual = "manual";
}

public static class MetadataProviderRoles
{
    public const string Primary = "primary";
    public const string Optional = "optional";
    public const string Future = "future";
}

public sealed record MetadataProviderCapability(
    string ProviderId,
    string DisplayName,
    string Kind,
    string Role,
    bool IsPrimary,
    bool IsOptional,
    bool IsFuture,
    bool IsEnabled,
    bool IsRegistered,
    bool IsConfigured,
    bool IsAvailable,
    bool SupportsSearch,
    bool SupportsDetails,
    string? Notes);

public sealed record MetadataProviderResolutionResponse(
    string RequestedCategory,
    string MacroCategory,
    bool ManualEntryOnly,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<MetadataProviderCapability> Providers);

public sealed record LiveMetadataSearchResultResponse(
    string Provider,
    string ProviderName,
    string Kind,
    string ExternalId,
    string Title,
    string? Subtitle,
    string? Description,
    string? ImageUrl,
    string? ReleaseDate,
    string? ExternalUrl,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record LiveMetadataDetailsResponse(
    string Provider,
    string ProviderName,
    string Kind,
    string ExternalId,
    string Title,
    string? OriginalTitle,
    string? Year,
    string? Description,
    IReadOnlyList<string> Genres,
    string? PosterUrl,
    string? BackdropUrl,
    int? RuntimeMinutes,
    string? ReleaseDate,
    decimal? ExternalRating,
    string? ExternalUrl,
    string SourceId,
    string SourceName,
    IReadOnlyList<ExternalMetadataAttribute> Attributes,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record ExternalMetadataSearchResult(
    string Provider,
    string Kind,
    string ExternalId,
    string Title,
    string? Subtitle,
    string? Description,
    string? ImageUrl,
    string? ReleaseDate,
    string? ExternalUrl,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record ExternalMetadataDetails(
    string Provider,
    string Kind,
    string ExternalId,
    string Title,
    string? Description,
    string? ImageUrl,
    string? ReleaseDate,
    string? ExternalUrl,
    IReadOnlyList<ExternalMetadataAttribute> Attributes,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record ExternalMetadataAttribute(string Key, string Label, string Value, string ValueType = "Text", string? Unit = null);

public sealed record ImportExternalItemRequest(Guid CollectionId, string Provider, string ExternalId);
