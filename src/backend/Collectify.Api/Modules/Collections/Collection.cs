namespace Collectify.Api.Modules.Collections;

public sealed record Collection(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    IReadOnlyList<CollectionItem> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
