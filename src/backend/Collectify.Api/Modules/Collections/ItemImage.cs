namespace Collectify.Api.Modules.Collections;

public sealed class ItemImage
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
