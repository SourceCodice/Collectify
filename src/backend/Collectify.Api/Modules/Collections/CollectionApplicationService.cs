namespace Collectify.Api.Modules.Collections;

public sealed class CollectionApplicationService(ICollectionRepository repository)
{
    public async Task<IReadOnlyList<CollectionSummaryResponse>> ListCollectionsAsync(CancellationToken cancellationToken)
    {
        var collections = await repository.ListAsync(cancellationToken);
        return collections.Select(collection => collection.ToSummaryResponse()).ToList();
    }

    public async Task<CollectionDetailResponse?> GetCollectionAsync(Guid id, CancellationToken cancellationToken)
    {
        var collection = await repository.GetAsync(id, cancellationToken);
        return collection?.ToDetailResponse();
    }

    public async Task<CollectionItemsResponse?> ListItemsAsync(Guid collectionId, CancellationToken cancellationToken)
    {
        var collection = await repository.GetAsync(collectionId, cancellationToken);
        return collection is null
            ? null
            : new CollectionItemsResponse(collection.Id, collection.Items.Select(item => item.ToResponse()).ToList());
    }

    public async Task<CollectionItemResponse?> GetItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken)
    {
        var collection = await repository.GetAsync(collectionId, cancellationToken);
        return collection?.Items.FirstOrDefault(item => item.Id == itemId)?.ToResponse();
    }

    public async Task<ValidationResult<CollectionDetailResponse>> CreateCollectionAsync(CreateCollectionRequest request, CancellationToken cancellationToken)
    {
        var errors = ValidateCollection(request.Name, request.Type);
        if (errors.Count > 0)
        {
            return ValidationResult<CollectionDetailResponse>.Failure(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Description = Normalize(request.Description),
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await repository.AddAsync(collection, cancellationToken);
        return ValidationResult<CollectionDetailResponse>.Success(created.ToDetailResponse());
    }

    public async Task<ValidationResult<CollectionDetailResponse?>> UpdateCollectionAsync(Guid id, UpdateCollectionRequest request, CancellationToken cancellationToken)
    {
        var errors = ValidateCollection(request.Name, request.Type);
        if (errors.Count > 0)
        {
            return ValidationResult<CollectionDetailResponse?>.Failure(errors);
        }

        var collection = await repository.GetAsync(id, cancellationToken);
        if (collection is null)
        {
            return ValidationResult<CollectionDetailResponse?>.Success(null);
        }

        collection.CategoryId = request.CategoryId;
        collection.Name = request.Name.Trim();
        collection.Type = request.Type.Trim();
        collection.Description = Normalize(request.Description);
        collection.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.SaveAsync(collection, cancellationToken);
        return ValidationResult<CollectionDetailResponse?>.Success(updated?.ToDetailResponse());
    }

    public Task<bool> DeleteCollectionAsync(Guid id, CancellationToken cancellationToken)
    {
        return repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<ValidationResult<CollectionItemResponse?>> AddItemAsync(Guid collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken)
    {
        var errors = ValidateItem(request);
        if (errors.Count > 0)
        {
            return ValidationResult<CollectionItemResponse?>.Failure(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var item = new Item
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            Notes = Normalize(request.Notes),
            Condition = string.IsNullOrWhiteSpace(request.Condition) ? "Non specificato" : request.Condition.Trim(),
            AcquiredAt = request.AcquiredAt,
            Attributes = BuildAttributes(request.Attributes, now),
            TagIds = request.TagIds?.Where(tagId => tagId != Guid.Empty).Distinct().ToList() ?? [],
            ExternalReferences = BuildExternalReferences(request.ExternalReferences, now),
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await repository.AddItemAsync(collectionId, item, cancellationToken);
        return ValidationResult<CollectionItemResponse?>.Success(created?.ToResponse());
    }

    public Task<bool> DeleteItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken)
    {
        return repository.DeleteItemAsync(collectionId, itemId, cancellationToken);
    }

    private static Dictionary<string, string[]> ValidateCollection(string name, string type)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors[nameof(CreateCollectionRequest.Name)] = ["Name is required."];
        }
        else if (name.Trim().Length > 120)
        {
            errors[nameof(CreateCollectionRequest.Name)] = ["Name cannot exceed 120 characters."];
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            errors[nameof(CreateCollectionRequest.Type)] = ["Type is required."];
        }
        else if (type.Trim().Length > 60)
        {
            errors[nameof(CreateCollectionRequest.Type)] = ["Type cannot exceed 60 characters."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateItem(AddCollectionItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors[nameof(AddCollectionItemRequest.Title)] = ["Title is required."];
        }
        else if (request.Title.Trim().Length > 160)
        {
            errors[nameof(AddCollectionItemRequest.Title)] = ["Title cannot exceed 160 characters."];
        }

        if (request.Attributes?.Any(attribute => string.IsNullOrWhiteSpace(attribute.Key) && !string.IsNullOrWhiteSpace(attribute.Value)) == true)
        {
            errors[nameof(AddCollectionItemRequest.Attributes)] = ["Attribute key is required when a value is provided."];
        }

        if (request.ExternalReferences?.Any(reference => string.IsNullOrWhiteSpace(reference.Provider) && (!string.IsNullOrWhiteSpace(reference.ExternalId) || !string.IsNullOrWhiteSpace(reference.Url))) == true)
        {
            errors[nameof(AddCollectionItemRequest.ExternalReferences)] = ["Provider is required when an external reference is provided."];
        }

        return errors;
    }

    private static List<ItemAttribute> BuildAttributes(IReadOnlyList<ItemAttributeRequest>? requests, DateTimeOffset now)
    {
        return requests?
            .Where(request => !string.IsNullOrWhiteSpace(request.Key))
            .Select(request => new ItemAttribute
            {
                Id = Guid.NewGuid(),
                Key = request.Key.Trim(),
                Label = string.IsNullOrWhiteSpace(request.Label) ? request.Key.Trim() : request.Label.Trim(),
                Value = request.Value.Trim(),
                ValueType = string.IsNullOrWhiteSpace(request.ValueType) ? "Text" : request.ValueType.Trim(),
                Unit = Normalize(request.Unit),
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList() ?? [];
    }

    private static List<ExternalReference> BuildExternalReferences(IReadOnlyList<ExternalReferenceRequest>? requests, DateTimeOffset now)
    {
        return requests?
            .Where(request => !string.IsNullOrWhiteSpace(request.Provider))
            .Select(request => new ExternalReference
            {
                Id = Guid.NewGuid(),
                Provider = request.Provider.Trim(),
                ExternalId = Normalize(request.ExternalId),
                Url = Normalize(request.Url),
                Metadata = request.Metadata ?? [],
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList() ?? [];
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
