namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class ExternalMetadataOptions
{
    public ExternalMetadataProviderOptions TMDb { get; set; } = new()
    {
        BaseUrl = "https://api.themoviedb.org/3",
        RequestsPerSecond = 4
    };

    public ExternalMetadataProviderOptions RAWG { get; set; } = new()
    {
        BaseUrl = "https://api.rawg.io/api",
        RequestsPerSecond = 4
    };

    public DiscogsProviderOptions Discogs { get; set; } = new()
    {
        BaseUrl = "https://api.discogs.com",
        RequestsPerSecond = 2
    };

    public ExternalMetadataRetryOptions Retry { get; set; } = new();
    public MetadataProviderResolverOptions ProviderResolver { get; set; } = new();
}

public class ExternalMetadataProviderOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public int RequestsPerSecond { get; set; } = 3;
}

public sealed class DiscogsProviderOptions : ExternalMetadataProviderOptions
{
    public string ApiSecret { get; set; } = string.Empty;
}

public sealed class ExternalMetadataRetryOptions
{
    public int MaxRetries { get; set; } = 2;
    public int InitialDelayMilliseconds { get; set; } = 500;
}

public sealed class MetadataProviderResolverOptions
{
    public Dictionary<string, MetadataCategoryProviderOptions> Categories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MetadataCategoryProviderOptions
{
    public bool ManualEntryOnly { get; set; }
    public List<string> Aliases { get; set; } = [];
    public List<MetadataProviderReferenceOptions> Providers { get; set; } = [];
}

public sealed class MetadataProviderReferenceOptions
{
    public string ProviderId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Role { get; set; } = MetadataProviderRoles.Primary;
    public bool IsEnabled { get; set; } = true;
    public string? Notes { get; set; }
}
