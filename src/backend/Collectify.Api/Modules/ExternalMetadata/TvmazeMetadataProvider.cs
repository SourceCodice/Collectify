using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed partial class TvmazeMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public string ProviderId => "tvmaze";
    public string SupportedKind => ExternalMetadataKinds.TvShow;

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<List<TvmazeSearchResult>>(
            BuildUrl(Options.TVmaze.BaseUrl, "search/shows", ("q", query)),
            Options.TVmaze,
            configureHeaders: null,
            cancellationToken);

        return response?
            .Where(result => result.Show.Id > 0 && !string.IsNullOrWhiteSpace(result.Show.Name))
            .Select(result => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                result.Show.Id.ToString(),
                Normalize(result.Show.Name),
                result.Show.Premiered,
                StripHtml(result.Show.Summary),
                result.Show.Image?.Original ?? result.Show.Image?.Medium,
                result.Show.Premiered,
                result.Show.Url,
                Metadata(
                    ("genres", string.Join(", ", result.Show.Genres)),
                    ("language", result.Show.Language),
                    ("status", result.Show.Status),
                    ("rating", result.Show.Rating?.Average?.ToString("0.0")))))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<TvmazeShow>(
            BuildUrl(Options.TVmaze.BaseUrl, $"shows/{externalId.Trim()}"),
            Options.TVmaze,
            configureHeaders: null,
            cancellationToken);

        if (response is null || response.Id == 0 || string.IsNullOrWhiteSpace(response.Name))
        {
            return null;
        }

        var attributes = new List<ExternalMetadataAttribute>
        {
            new("releaseDate", "Prima uscita", Normalize(response.Premiered), "Date"),
            new("genres", "Generi", string.Join(", ", response.Genres)),
            new("runtime", "Durata episodio", response.Runtime?.ToString() ?? string.Empty, "Number", "min"),
            new("rating", "Rating TVmaze", response.Rating?.Average?.ToString("0.0") ?? string.Empty, "Number"),
            new("language", "Lingua", Normalize(response.Language)),
            new("status", "Stato", Normalize(response.Status)),
            new("network", "Network", response.Network?.Name ?? response.WebChannel?.Name ?? string.Empty)
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            response.Id.ToString(),
            Normalize(response.Name),
            StripHtml(response.Summary),
            response.Image?.Original ?? response.Image?.Medium,
            response.Premiered,
            response.Url,
            attributes,
            Metadata(
                ("genres", string.Join(", ", response.Genres)),
                ("language", response.Language),
                ("status", response.Status),
                ("officialSite", response.OfficialSite)));
    }

    private static string? StripHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return HtmlRegex().Replace(value, string.Empty).Trim();
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlRegex();

    private sealed class TvmazeSearchResult
    {
        public double Score { get; set; }
        public TvmazeShow Show { get; set; } = new();
    }

    private sealed class TvmazeShow
    {
        public int Id { get; set; }
        public string? Url { get; set; }
        public string? Name { get; set; }
        public string? Language { get; set; }
        public List<string> Genres { get; set; } = [];
        public string? Status { get; set; }
        public int? Runtime { get; set; }
        public string? Premiered { get; set; }
        [JsonPropertyName("officialSite")]
        public string? OfficialSite { get; set; }
        public TvmazeRating? Rating { get; set; }
        public TvmazeImage? Image { get; set; }
        public string? Summary { get; set; }
        public TvmazeNetwork? Network { get; set; }
        [JsonPropertyName("webChannel")]
        public TvmazeNetwork? WebChannel { get; set; }
    }

    private sealed class TvmazeRating
    {
        public double? Average { get; set; }
    }

    private sealed class TvmazeImage
    {
        public string? Medium { get; set; }
        public string? Original { get; set; }
    }

    private sealed class TvmazeNetwork
    {
        public string? Name { get; set; }
    }
}
