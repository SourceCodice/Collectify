using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class ExternalMetadataApplicationService(
    IEnumerable<IExternalMetadataProvider> providers,
    ICollectionRepository collectionRepository)
{
    private readonly IReadOnlyList<IExternalMetadataProvider> _providers = providers.ToList();

    public Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string kind, string query, CancellationToken cancellationToken)
    {
        var provider = GetProviderByKind(kind);
        return provider.SearchAsync(query.Trim(), cancellationToken);
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

        var collection = await collectionRepository.GetAsync(request.CollectionId, cancellationToken);
        if (collection is null)
        {
            return ValidationResult<CollectionItemResponse?>.Success(null);
        }

        var now = DateTimeOffset.UtcNow;
        var item = new Item
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
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

        var created = await collectionRepository.AddItemAsync(collection.Id, item, cancellationToken);
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
