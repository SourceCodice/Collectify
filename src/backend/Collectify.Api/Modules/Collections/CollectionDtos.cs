namespace Collectify.Api.Modules.Collections;

public sealed record CreateCollectionRequest(string Name, string Type, string? Description);

public sealed record UpdateCollectionRequest(string Name, string Type, string? Description);

public sealed record AddCollectionItemRequest(string Title, string? Notes, string? Condition, DateTimeOffset? AcquiredAt);

public sealed record CollectionSummaryResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    int ItemCount,
    DateTimeOffset UpdatedAt);

public sealed record CollectionDetailResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    IReadOnlyList<CollectionItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CollectionItemResponse(
    Guid Id,
    string Title,
    string? Notes,
    string Condition,
    DateTimeOffset AcquiredAt,
    DateTimeOffset UpdatedAt);
