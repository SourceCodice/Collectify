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
