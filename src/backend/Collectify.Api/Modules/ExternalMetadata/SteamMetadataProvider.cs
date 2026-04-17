using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class SteamMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public string ProviderId => "steam";
    public string SupportedKind => ExternalMetadataKinds.Game;

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<SteamSearchResponse>(
            BuildUrl(
                Options.Steam.BaseUrl,
                "storesearch",
                ("term", query),
                ("l", "italian"),
                ("cc", "IT")),
            Options.Steam,
            configureHeaders: null,
            cancellationToken);

        return response?.Items
            .Where(game => game.Id > 0 && !string.IsNullOrWhiteSpace(game.Name))
            .Select(game => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                game.Id.ToString(),
                Normalize(game.Name),
                null,
                null,
                game.TinyImage,
                null,
                $"https://store.steampowered.com/app/{game.Id}",
                Metadata(("price", game.Price?.FinalFormatted))))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        var appId = externalId.Trim();
        var response = await GetJsonAsync<Dictionary<string, SteamAppDetailsEnvelope>>(
            BuildUrl(
                Options.Steam.BaseUrl,
                "appdetails",
                ("appids", appId),
                ("l", "italian"),
                ("cc", "IT")),
            Options.Steam,
            configureHeaders: null,
            cancellationToken);
        var details = response?.TryGetValue(appId, out var envelope) == true && envelope.Success ? envelope.Data : null;

        if (details is null || details.SteamAppId == 0 || string.IsNullOrWhiteSpace(details.Name))
        {
            return null;
        }

        var platforms = new[]
        {
            details.Platforms.Windows ? "Windows" : null,
            details.Platforms.Mac ? "macOS" : null,
            details.Platforms.Linux ? "Linux" : null
        }.Where(platform => !string.IsNullOrWhiteSpace(platform));
        var attributes = new List<ExternalMetadataAttribute>
        {
            new("releaseDate", "Data uscita", Normalize(details.ReleaseDate?.Date), "Date"),
            new("genres", "Generi", string.Join(", ", details.Genres.Select(genre => genre.Description))),
            new("developers", "Sviluppatori", string.Join(", ", details.Developers)),
            new("publishers", "Publisher", string.Join(", ", details.Publishers)),
            new("platforms", "Piattaforme", string.Join(", ", platforms)),
            new("metacritic", "Metacritic", details.Metacritic?.Score.ToString() ?? string.Empty, "Number")
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            details.SteamAppId.ToString(),
            Normalize(details.Name),
            NormalizeOptional(details.ShortDescription),
            details.HeaderImage,
            details.ReleaseDate?.Date,
            $"https://store.steampowered.com/app/{details.SteamAppId}",
            attributes,
            Metadata(
                ("website", details.Website),
                ("background", details.Background),
                ("metacriticUrl", details.Metacritic?.Url)));
    }

    private sealed class SteamSearchResponse
    {
        public List<SteamSearchItem> Items { get; set; } = [];
    }

    private sealed class SteamSearchItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("tiny_image")]
        public string? TinyImage { get; set; }
        public SteamSearchPrice? Price { get; set; }
    }

    private sealed class SteamSearchPrice
    {
        [JsonPropertyName("final_formatted")]
        public string? FinalFormatted { get; set; }
    }

    private sealed class SteamAppDetailsEnvelope
    {
        public bool Success { get; set; }
        public SteamAppDetails? Data { get; set; }
    }

    private sealed class SteamAppDetails
    {
        [JsonPropertyName("steam_appid")]
        public int SteamAppId { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("short_description")]
        public string? ShortDescription { get; set; }
        [JsonPropertyName("header_image")]
        public string? HeaderImage { get; set; }
        public string? Website { get; set; }
        public string? Background { get; set; }
        [JsonPropertyName("release_date")]
        public SteamReleaseDate? ReleaseDate { get; set; }
        public SteamPlatforms Platforms { get; set; } = new();
        public List<string> Developers { get; set; } = [];
        public List<string> Publishers { get; set; } = [];
        public List<SteamGenre> Genres { get; set; } = [];
        public SteamMetacritic? Metacritic { get; set; }
    }

    private sealed class SteamReleaseDate
    {
        public string? Date { get; set; }
    }

    private sealed class SteamPlatforms
    {
        public bool Windows { get; set; }
        public bool Mac { get; set; }
        public bool Linux { get; set; }
    }

    private sealed class SteamGenre
    {
        public string Description { get; set; } = string.Empty;
    }

    private sealed class SteamMetacritic
    {
        public int Score { get; set; }
        public string? Url { get; set; }
    }
}
