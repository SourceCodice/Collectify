namespace Collectify.Api.Modules.Collections;

public sealed class ExternalReference
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
