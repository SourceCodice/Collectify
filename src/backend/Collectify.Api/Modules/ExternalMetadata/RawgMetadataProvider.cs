using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class RawgMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public string ProviderId => "rawg";
    public string SupportedKind => ExternalMetadataKinds.Game;

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var response = await GetJsonAsync<RawgSearchResponse>(
            BuildUrl(
                Options.RAWG.BaseUrl,
                "games",
                ("key", Options.RAWG.ApiKey),
                ("search", query),
                ("page_size", "12")),
            Options.RAWG,
            configureHeaders: null,
            cancellationToken);

        return response?.Results
            .Where(game => game.Id > 0 && !string.IsNullOrWhiteSpace(game.Name))
            .Select(game => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                game.Id.ToString(),
                Normalize(game.Name),
                game.Released,
                null,
                game.BackgroundImage,
                game.Released,
                string.IsNullOrWhiteSpace(game.Slug) ? null : $"https://rawg.io/games/{game.Slug}",
                Metadata(("rating", game.Rating?.ToString("0.0")), ("metacritic", game.Metacritic?.ToString()))))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var response = await GetJsonAsync<RawgGameDetails>(
            BuildUrl(Options.RAWG.BaseUrl, $"games/{externalId}", ("key", Options.RAWG.ApiKey)),
            Options.RAWG,
            configureHeaders: null,
            cancellationToken);

        if (response is null || response.Id == 0 || string.IsNullOrWhiteSpace(response.Name))
        {
            return null;
        }

        var attributes = new List<ExternalMetadataAttribute>
        {
            new("releaseDate", "Data uscita", Normalize(response.Released), "Date"),
            new("genres", "Generi", string.Join(", ", response.Genres.Select(item => item.Name))),
            new("platforms", "Piattaforme", string.Join(", ", response.Platforms.Select(item => item.Platform.Name))),
            new("developers", "Sviluppatori", string.Join(", ", response.Developers.Select(item => item.Name))),
            new("publishers", "Publisher", string.Join(", ", response.Publishers.Select(item => item.Name))),
            new("rating", "Valutazione RAWG", response.Rating?.ToString("0.0") ?? string.Empty, "Number"),
            new("metacritic", "Metacritic", response.Metacritic?.ToString() ?? string.Empty, "Number"),
            new("playtime", "Tempo medio", response.Playtime?.ToString() ?? string.Empty, "Number", "h")
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            response.Id.ToString(),
            Normalize(response.Name),
            NormalizeOptional(response.DescriptionRaw),
            response.BackgroundImage,
            response.Released,
            string.IsNullOrWhiteSpace(response.Slug) ? null : $"https://rawg.io/games/{response.Slug}",
            attributes,
            Metadata(("website", response.Website), ("slug", response.Slug)));
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(Options.RAWG.ApiKey))
        {
            throw new ExternalMetadataProviderException("RAWG API key is missing.", StatusCodes.Status503ServiceUnavailable);
        }
    }

    private sealed class RawgSearchResponse
    {
        public List<RawgGameSummary> Results { get; set; } = [];
    }

    private class RawgGameSummary
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Released { get; set; }
        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }
        public double? Rating { get; set; }
        public int? Metacritic { get; set; }
    }

    private sealed class RawgGameDetails : RawgGameSummary
    {
        [JsonPropertyName("description_raw")]
        public string? DescriptionRaw { get; set; }
        public string? Website { get; set; }
        public int? Playtime { get; set; }
        public List<NamedRawgValue> Genres { get; set; } = [];
        public List<RawgPlatformWrapper> Platforms { get; set; } = [];
        public List<NamedRawgValue> Developers { get; set; } = [];
        public List<NamedRawgValue> Publishers { get; set; } = [];
    }

    private sealed class NamedRawgValue
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class RawgPlatformWrapper
    {
        public NamedRawgValue Platform { get; set; } = new();
    }
}
