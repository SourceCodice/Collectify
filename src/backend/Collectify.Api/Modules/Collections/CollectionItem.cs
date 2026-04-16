namespace Collectify.Api.Modules.Collections;

public sealed record CollectionItem(
    Guid Id,
    string Title,
    string? Notes,
    string Condition,
    DateTimeOffset AcquiredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
