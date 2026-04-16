using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Persistence;

public sealed class CollectifyDataDocument
{
    public int SchemaVersion { get; set; } = 1;
    public UserProfile UserProfile { get; set; } = new();
    public AppSettings AppSettings { get; set; } = new();
    public List<CollectionCategory> Categories { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
    public List<Collection> Collections { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
