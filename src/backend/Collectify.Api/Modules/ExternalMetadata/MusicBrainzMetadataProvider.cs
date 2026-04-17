using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class MusicBrainzMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public string ProviderId => "musicbrainz";
    public string SupportedKind => ExternalMetadataKinds.Album;

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<MusicBrainzReleaseGroupSearchResponse>(
            BuildUrl(
                Options.MusicBrainz.BaseUrl,
                "release-group",
                ("query", query),
                ("fmt", "json"),
                ("limit", "12")),
            Options.MusicBrainz,
            configureHeaders: null,
            cancellationToken);

        return response?.ReleaseGroups
            .Where(album => !string.IsNullOrWhiteSpace(album.Id) && !string.IsNullOrWhiteSpace(album.Title))
            .Select(album => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                Normalize(album.Id),
                Normalize(album.Title),
                album.FirstReleaseDate,
                BuildArtistCredit(album.ArtistCredit),
                BuildCoverArtUrl(album.Id),
                album.FirstReleaseDate,
                $"https://musicbrainz.org/release-group/{album.Id}",
                Metadata(
                    ("artists", BuildArtistCredit(album.ArtistCredit)),
                    ("primaryType", album.PrimaryType),
                    ("score", album.Score?.ToString()))))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<MusicBrainzReleaseGroupDetails>(
            BuildUrl(
                Options.MusicBrainz.BaseUrl,
                $"release-group/{externalId.Trim()}",
                ("fmt", "json"),
                ("inc", "artists+genres+tags+releases")),
            Options.MusicBrainz,
            configureHeaders: null,
            cancellationToken);

        if (response is null || string.IsNullOrWhiteSpace(response.Id) || string.IsNullOrWhiteSpace(response.Title))
        {
            return null;
        }

        var genres = response.Genres.Count > 0
            ? response.Genres.Select(genre => genre.Name)
            : response.Tags.OrderByDescending(tag => tag.Count).Select(tag => tag.Name);
        var attributes = new List<ExternalMetadataAttribute>
        {
            new("releaseDate", "Prima uscita", Normalize(response.FirstReleaseDate), "Date"),
            new("artists", "Artisti", BuildArtistCredit(response.ArtistCredit)),
            new("genres", "Generi", string.Join(", ", genres.Take(10))),
            new("primaryType", "Tipo", Normalize(response.PrimaryType)),
            new("releases", "Edizioni", response.Releases.Count.ToString(), "Number")
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            Normalize(response.Id),
            Normalize(response.Title),
            null,
            BuildCoverArtUrl(response.Id),
            response.FirstReleaseDate,
            $"https://musicbrainz.org/release-group/{response.Id}",
            attributes,
            Metadata(
                ("artists", BuildArtistCredit(response.ArtistCredit)),
                ("primaryType", response.PrimaryType),
                ("coverArtArchive", $"https://coverartarchive.org/release-group/{response.Id}")));
    }

    private static string BuildArtistCredit(IReadOnlyList<MusicBrainzArtistCredit> artistCredit)
    {
        return string.Join("", artistCredit.Select(credit => string.IsNullOrWhiteSpace(credit.Name) ? credit.JoinPhrase : $"{credit.Name}{credit.JoinPhrase}")).Trim();
    }

    private static string? BuildCoverArtUrl(string? releaseGroupId)
    {
        return string.IsNullOrWhiteSpace(releaseGroupId)
            ? null
            : $"https://coverartarchive.org/release-group/{releaseGroupId}/front-500";
    }

    private sealed class MusicBrainzReleaseGroupSearchResponse
    {
        [JsonPropertyName("release-groups")]
        public List<MusicBrainzReleaseGroupSummary> ReleaseGroups { get; set; } = [];
    }

    private class MusicBrainzReleaseGroupSummary
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public int? Score { get; set; }
        [JsonPropertyName("first-release-date")]
        public string? FirstReleaseDate { get; set; }
        [JsonPropertyName("primary-type")]
        public string? PrimaryType { get; set; }
        [JsonPropertyName("artist-credit")]
        public List<MusicBrainzArtistCredit> ArtistCredit { get; set; } = [];
    }

    private sealed class MusicBrainzReleaseGroupDetails : MusicBrainzReleaseGroupSummary
    {
        public List<MusicBrainzGenre> Genres { get; set; } = [];
        public List<MusicBrainzTag> Tags { get; set; } = [];
        public List<MusicBrainzRelease> Releases { get; set; } = [];
    }

    private sealed class MusicBrainzArtistCredit
    {
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("joinphrase")]
        public string? JoinPhrase { get; set; }
    }

    private class MusicBrainzGenre
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class MusicBrainzTag : MusicBrainzGenre
    {
        public int Count { get; set; }
    }

    private sealed class MusicBrainzRelease
    {
        public string Id { get; set; } = string.Empty;
    }
}
