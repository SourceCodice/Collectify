namespace Collectify.Api.Modules.Collections;

public sealed record CreateCollectionRequest(string Name, string Type, string? Description);

public sealed record UpdateCollectionRequest(string Name, string Type, string? Description);

public sealed record AddCollectionItemRequest(
    string Title,
    string? Description,
    string? Notes,
    string? Condition,
    DateTimeOffset? AcquiredAt,
    IReadOnlyList<ItemAttributeRequest>? Attributes,
    IReadOnlyList<Guid>? TagIds,
    IReadOnlyList<ExternalReferenceRequest>? ExternalReferences);

public sealed record ItemAttributeRequest(string Key, string Label, string Value, string? ValueType, string? Unit);

public sealed record ExternalReferenceRequest(string Provider, string? ExternalId, string? Url, Dictionary<string, string>? Metadata);

public sealed record CollectionSummaryResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    Guid? CategoryId,
    int ItemCount,
    DateTimeOffset UpdatedAt);

public sealed record CollectionDetailResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    Guid? CategoryId,
    IReadOnlyList<CollectionItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CollectionItemResponse(
    Guid Id,
    string Title,
    string? Description,
    string? Notes,
    string Condition,
    DateTimeOffset? AcquiredAt,
    IReadOnlyList<ItemAttributeResponse> Attributes,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<ExternalReferenceResponse> ExternalReferences,
    DateTimeOffset UpdatedAt);

public sealed record ItemAttributeResponse(Guid Id, string Key, string Label, string Value, string ValueType, string? Unit);

public sealed record ExternalReferenceResponse(Guid Id, string Provider, string? ExternalId, string? Url, Dictionary<string, string> Metadata);
