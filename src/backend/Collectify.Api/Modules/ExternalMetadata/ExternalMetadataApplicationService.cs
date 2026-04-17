using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class ExternalMetadataApplicationService(
    IEnumerable<IExternalMetadataProvider> providers,
    ICollectionRepository collectionRepository,
    MetadataProviderResolver providerResolver)
{
    private readonly IReadOnlyList<IExternalMetadataProvider> _providers = providers.ToList();

    public Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string kind, string query, CancellationToken cancellationToken)
    {
        var provider = GetProviderByKind(kind);
        return provider.SearchAsync(query.Trim(), cancellationToken);
    }

    public MetadataProviderResolutionResponse ResolveProviders(string itemTypeOrMacroCategory)
    {
        return providerResolver.Resolve(itemTypeOrMacroCategory);
    }

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchByCategoryAsync(
        string itemTypeOrMacroCategory,
        string query,
        CancellationToken cancellationToken)
    {
        var resolvedProviders = providerResolver.ResolveAvailableProviders(itemTypeOrMacroCategory);

        if (resolvedProviders.Count == 0)
        {
            return [];
        }

        var results = new List<ExternalMetadataSearchResult>();
        foreach (var provider in resolvedProviders)
        {
            var providerResults = await provider.SearchAsync(query.Trim(), cancellationToken);
            results.AddRange(providerResults);
        }

        return results;
    }

    public async Task<IReadOnlyList<LiveMetadataSearchResultResponse>> SearchLiveAsync(
        string itemTypeOrMacroCategory,
        string query,
        string? providerId,
        CancellationToken cancellationToken)
    {
        if (query.Trim().Length < 2)
        {
            return [];
        }

        var resolution = providerResolver.Resolve(itemTypeOrMacroCategory);
        var capabilitiesByProvider = resolution.Providers.ToDictionary(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase);
        var resolvedProviders = string.IsNullOrWhiteSpace(providerId)
            ? providerResolver.ResolveAvailableProviders(itemTypeOrMacroCategory)
            : ResolveSelectedProvider(itemTypeOrMacroCategory, providerId);

        if (resolvedProviders.Count == 0)
        {
            return [];
        }

        var results = new List<LiveMetadataSearchResultResponse>();
        foreach (var provider in resolvedProviders)
        {
            var providerResults = await provider.SearchAsync(query.Trim(), cancellationToken);
            var providerName = capabilitiesByProvider.TryGetValue(provider.ProviderId, out var capability)
                ? capability.DisplayName
                : provider.ProviderId;

            results.AddRange(providerResults.Select(result => ToLiveSearchResult(result, providerName)));
        }

        return results;
    }

    private IReadOnlyList<IExternalMetadataProvider> ResolveSelectedProvider(string itemTypeOrMacroCategory, string providerId)
    {
        var provider = providerResolver.ResolveAvailableProvider(itemTypeOrMacroCategory, providerId);
        return provider is null ? [] : [provider];
    }

    public async Task<LiveMetadataDetailsResponse?> GetLiveDetailsAsync(
        string itemTypeOrMacroCategory,
        string providerId,
        string externalId,
        CancellationToken cancellationToken)
    {
        var provider = providerResolver.ResolveAvailableProvider(itemTypeOrMacroCategory, providerId)
            ?? throw new ExternalMetadataProviderException(
                $"Provider '{providerId}' is not available for '{itemTypeOrMacroCategory}'.",
                StatusCodes.Status404NotFound);
        var capability = providerResolver.ResolveCapability(itemTypeOrMacroCategory, provider.ProviderId);
        var details = await provider.GetDetailsAsync(externalId.Trim(), cancellationToken);

        return details is null ? null : ToLiveDetails(details, capability?.DisplayName ?? provider.ProviderId);
    }

    public Task<ExternalMetadataDetails?> GetDetailsAsync(string providerId, string externalId, CancellationToken cancellationToken)
    {
        var provider = GetProviderById(providerId);
        return provider.GetDetailsAsync(externalId.Trim(), cancellationToken);
    }

    public async Task<ValidationResult<CollectionItemResponse?>> ImportAsync(ImportExternalItemRequest request, CancellationToken cancellationToken)
    {
        var errors = ValidateImportRequest(request);
        if (errors.Count > 0)
        {
            return ValidationResult<CollectionItemResponse?>.Failure(errors);
        }

        var provider = GetProviderById(request.Provider);
        var details = await provider.GetDetailsAsync(request.ExternalId.Trim(), cancellationToken);
        if (details is null)
        {
            return ValidationResult<CollectionItemResponse?>.Success(null);
        }

        var now = DateTimeOffset.UtcNow;
        var item = new Item
        {
            Id = Guid.NewGuid(),
            CollectionId = request.CollectionId,
            Title = details.Title,
            Description = details.Description,
            Notes = details.ExternalUrl is null ? null : $"Importato da {details.Provider}: {details.ExternalUrl}",
            Condition = "Importato",
            Attributes = details.Attributes.Select(attribute => new ItemAttribute
            {
                Id = Guid.NewGuid(),
                Key = attribute.Key,
                Label = attribute.Label,
                Value = attribute.Value,
                ValueType = attribute.ValueType,
                Unit = attribute.Unit,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList(),
            ExternalReferences = [BuildExternalReference(details, now)],
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await collectionRepository.AddItemAsync(request.CollectionId, item, cancellationToken);
        return ValidationResult<CollectionItemResponse?>.Success(created?.ToResponse());
    }

    private IExternalMetadataProvider GetProviderByKind(string kind)
    {
        return _providers.FirstOrDefault(provider => string.Equals(provider.SupportedKind, kind, StringComparison.OrdinalIgnoreCase))
            ?? throw new ExternalMetadataProviderException($"No external metadata provider is registered for kind '{kind}'.", StatusCodes.Status404NotFound);
    }

    private IExternalMetadataProvider GetProviderById(string providerId)
    {
        return _providers.FirstOrDefault(provider => string.Equals(provider.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ExternalMetadataProviderException($"Unknown external metadata provider '{providerId}'.", StatusCodes.Status404NotFound);
    }

    private static ExternalReference BuildExternalReference(ExternalMetadataDetails details, DateTimeOffset now)
    {
        var metadata = details.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value);
        metadata["kind"] = details.Kind;

        if (!string.IsNullOrWhiteSpace(details.ImageUrl))
        {
            metadata["imageUrl"] = details.ImageUrl;
        }

        if (!string.IsNullOrWhiteSpace(details.ReleaseDate))
        {
            metadata["releaseDate"] = details.ReleaseDate;
        }

        return new ExternalReference
        {
            Id = Guid.NewGuid(),
            Provider = details.Provider,
            ExternalId = details.ExternalId,
            Url = details.ExternalUrl,
            Metadata = metadata,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static LiveMetadataSearchResultResponse ToLiveSearchResult(ExternalMetadataSearchResult result, string providerName)
    {
        return new LiveMetadataSearchResultResponse(
            result.Provider,
            providerName,
            result.Kind,
            result.ExternalId,
            result.Title,
            result.Subtitle,
            result.Description,
            result.ImageUrl,
            result.ReleaseDate,
            result.ExternalUrl,
            result.Metadata);
    }

    private static LiveMetadataDetailsResponse ToLiveDetails(ExternalMetadataDetails details, string providerName)
    {
        var metadata = details.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var genres = FindAttribute(details, "genres")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .ToList() ?? [];
        var releaseDate = details.ReleaseDate ?? FindAttribute(details, "releaseDate");

        return new LiveMetadataDetailsResponse(
            details.Provider,
            providerName,
            details.Kind,
            details.ExternalId,
            details.Title,
            ReadMetadata(metadata, "originalTitle"),
            ResolveYear(releaseDate),
            details.Description,
            genres,
            details.ImageUrl,
            ReadMetadata(metadata, "backdropUrl"),
            ResolveInt(FindAttribute(details, "runtime")),
            releaseDate,
            ResolveDecimal(FindAttribute(details, "rating") ?? FindAttribute(details, "voteAverage")),
            details.ExternalUrl,
            details.ExternalId,
            providerName,
            details.Attributes,
            metadata);
    }

    private static string? FindAttribute(ExternalMetadataDetails details, string key)
    {
        return details.Attributes.FirstOrDefault(attribute =>
            string.Equals(attribute.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private static string? ReadMetadata(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    private static string? ResolveYear(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Length < 4 ? null : value[..4];
    }

    private static int? ResolveInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static decimal? ResolveDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(
            value.Replace(',', '.'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static Dictionary<string, string[]> ValidateImportRequest(ImportExternalItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CollectionId == Guid.Empty)
        {
            errors[nameof(ImportExternalItemRequest.CollectionId)] = ["Collection id is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            errors[nameof(ImportExternalItemRequest.Provider)] = ["Provider is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            errors[nameof(ImportExternalItemRequest.ExternalId)] = ["External id is required."];
        }

        return errors;
    }
}
