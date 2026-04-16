using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class DiscogsMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public string ProviderId => "discogs";
    public string SupportedKind => ExternalMetadataKinds.Album;

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var response = await GetJsonAsync<DiscogsSearchResponse>(
            BuildUrl(
                Options.Discogs.BaseUrl,
                "database/search",
                ("q", query),
                ("type", "master"),
                ("key", Options.Discogs.ApiKey),
                ("secret", Options.Discogs.ApiSecret)),
            Options.Discogs,
            configureHeaders: null,
            cancellationToken);

        return response?.Results
            .Where(album => album.Id > 0 && !string.IsNullOrWhiteSpace(album.Title))
            .Select(album => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                album.Id.ToString(),
                Normalize(album.Title),
                album.Year?.ToString(),
                string.Join(", ", album.Genre.Concat(album.Style)),
                album.CoverImage,
                album.Year?.ToString(),
                $"https://www.discogs.com/master/{album.Id}",
                Metadata(("country", album.Country), ("label", string.Join(", ", album.Label)))))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var response = await GetJsonAsync<DiscogsMasterDetails>(
            BuildUrl(
                Options.Discogs.BaseUrl,
                $"masters/{externalId}",
                ("key", Options.Discogs.ApiKey),
                ("secret", Options.Discogs.ApiSecret)),
            Options.Discogs,
            configureHeaders: null,
            cancellationToken);

        if (response is null || response.Id == 0 || string.IsNullOrWhiteSpace(response.Title))
        {
            return null;
        }

        var imageUrl = response.Images.FirstOrDefault(image => !string.IsNullOrWhiteSpace(image.Uri))?.Uri;
        var attributes = new List<ExternalMetadataAttribute>
        {
            new("year", "Anno", response.Year?.ToString() ?? string.Empty, "Number"),
            new("artists", "Artisti", string.Join(", ", response.Artists.Select(artist => artist.Name))),
            new("genres", "Generi", string.Join(", ", response.Genres)),
            new("styles", "Stili", string.Join(", ", response.Styles)),
            new("tracks", "Tracce", response.Tracklist.Count.ToString(), "Number")
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            response.Id.ToString(),
            Normalize(response.Title),
            BuildTrackDescription(response.Tracklist),
            imageUrl,
            response.Year?.ToString(),
            $"https://www.discogs.com/master/{response.Id}",
            attributes,
            Metadata(("discogsUri", response.Uri), ("resourceUrl", response.ResourceUrl)));
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(Options.Discogs.ApiKey) || string.IsNullOrWhiteSpace(Options.Discogs.ApiSecret))
        {
            throw new ExternalMetadataProviderException("Discogs API key or secret is missing.", StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static string? BuildTrackDescription(IReadOnlyList<DiscogsTrack> tracks)
    {
        if (tracks.Count == 0)
        {
            return null;
        }

        return string.Join(Environment.NewLine, tracks.Take(12).Select(track => $"{track.Position} {track.Title}".Trim()));
    }

    private sealed class DiscogsSearchResponse
    {
        public List<DiscogsSearchResult> Results { get; set; } = [];
    }

    private sealed class DiscogsSearchResult
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int? Year { get; set; }
        public string? Country { get; set; }
        public List<string> Label { get; set; } = [];
        public List<string> Genre { get; set; } = [];
        public List<string> Style { get; set; } = [];
        [JsonPropertyName("cover_image")]
        public string? CoverImage { get; set; }
    }

    private sealed class DiscogsMasterDetails
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int? Year { get; set; }
        public string? Uri { get; set; }
        [JsonPropertyName("resource_url")]
        public string? ResourceUrl { get; set; }
        public List<string> Genres { get; set; } = [];
        public List<string> Styles { get; set; } = [];
        public List<DiscogsArtist> Artists { get; set; } = [];
        public List<DiscogsImage> Images { get; set; } = [];
        public List<DiscogsTrack> Tracklist { get; set; } = [];
    }

    private sealed class DiscogsArtist
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class DiscogsImage
    {
        public string? Uri { get; set; }
    }

    private sealed class DiscogsTrack
    {
        public string Position { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
