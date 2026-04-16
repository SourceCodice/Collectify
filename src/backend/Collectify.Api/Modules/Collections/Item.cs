namespace Collectify.Api.Modules.Collections;

public sealed class Item
{
    public Guid Id { get; set; }
    public Guid CollectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string Condition { get; set; } = "Non specificato";
    public DateTimeOffset? AcquiredAt { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? Currency { get; set; }
    public List<ItemAttribute> Attributes { get; set; } = [];
    public List<Guid> TagIds { get; set; } = [];
    public List<ItemImage> Images { get; set; } = [];
    public List<ExternalReference> ExternalReferences { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
