namespace Collectify.Api.Modules.ExternalMetadata;

public static class ExternalMetadataKinds
{
    public const string Movie = "movie";
    public const string Game = "game";
    public const string Album = "album";
}

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
