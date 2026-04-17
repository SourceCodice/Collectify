using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class ItunesMovieMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ItunesMetadataProviderBase(httpClientFactory, options)
{
    public override string ProviderId => "itunes-movies";
    public override string SupportedKind => ExternalMetadataKinds.Movie;
    protected override string Media => "movie";
    protected override string Entity => "movie";
}

public sealed class ItunesAlbumMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ItunesMetadataProviderBase(httpClientFactory, options)
{
    public override string ProviderId => "itunes-albums";
    public override string SupportedKind => ExternalMetadataKinds.Album;
    protected override string Media => "music";
    protected override string Entity => "album";
}

public sealed class ItunesSingleMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ItunesMetadataProviderBase(httpClientFactory, options)
{
    public override string ProviderId => "itunes-singles";
    public override string SupportedKind => ExternalMetadataKinds.Single;
    protected override string Media => "music";
    protected override string Entity => "album";
}

public sealed class ItunesBookMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ItunesMetadataProviderBase(httpClientFactory, options)
{
    public override string ProviderId => "itunes-books";
    public override string SupportedKind => ExternalMetadataKinds.Book;
    protected override string Media => "ebook";
    protected override string Entity => "ebook";
}

public abstract class ItunesMetadataProviderBase(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public abstract string ProviderId { get; }
    public abstract string SupportedKind { get; }
    protected abstract string Media { get; }
    protected abstract string Entity { get; }

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<ItunesSearchResponse>(
            BuildUrl(
                Options.ITunes.BaseUrl,
                "search",
                ("term", query),
                ("media", Media),
                ("entity", Entity),
                ("country", "IT"),
                ("limit", "12")),
            Options.ITunes,
            configureHeaders: null,
            cancellationToken);

        return response?.Results
            .Where(item => ResolveId(item) is not null && !string.IsNullOrWhiteSpace(ResolveTitle(item)))
            .Where(IsResultAllowed)
            .Select(item => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                ResolveId(item)!,
                ResolveTitle(item)!,
                ResolveSubtitle(item),
                ResolveDescription(item),
                NormalizeArtwork(item.ArtworkUrl100),
                item.ReleaseDate,
                item.TrackViewUrl ?? item.CollectionViewUrl,
                BuildMetadata(item)))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<ItunesSearchResponse>(
            BuildUrl(
                Options.ITunes.BaseUrl,
                "lookup",
                ("id", externalId.Trim()),
                ("country", "IT")),
            Options.ITunes,
            configureHeaders: null,
            cancellationToken);
        var item = response?.Results.FirstOrDefault();

        if (item is null || string.IsNullOrWhiteSpace(ResolveTitle(item)) || !IsResultAllowed(item))
        {
            return null;
        }

        var attributes = new List<ExternalMetadataAttribute>
        {
            new("releaseDate", "Data uscita", Normalize(item.ReleaseDate), "Date"),
            new("artist", "Artista", Normalize(item.ArtistName)),
            new("genres", "Generi", string.Join(", ", item.Genres.Count > 0 ? item.Genres : [item.PrimaryGenreName ?? string.Empty])),
            new("runtime", "Durata", ResolveRuntimeMinutes(item)?.ToString() ?? string.Empty, "Number", "min"),
            new("rating", "Rating Apple", item.AverageUserRating?.ToString("0.0") ?? string.Empty, "Number"),
            new("trackCount", "Tracce", item.TrackCount?.ToString() ?? string.Empty, "Number")
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            ResolveId(item) ?? externalId.Trim(),
            ResolveTitle(item)!,
            ResolveDescription(item),
            NormalizeArtwork(item.ArtworkUrl100),
            item.ReleaseDate,
            item.TrackViewUrl ?? item.CollectionViewUrl,
            attributes,
            BuildMetadata(item));
    }

    private static string? ResolveId(ItunesResult item)
    {
        return (item.TrackId ?? item.CollectionId)?.ToString();
    }

    private string? ResolveTitle(ItunesResult item)
    {
        return SupportedKind is ExternalMetadataKinds.Album or ExternalMetadataKinds.Single
            ? NormalizeOptional(item.CollectionName ?? item.TrackName)
            : NormalizeOptional(item.TrackName ?? item.CollectionName);
    }

    private string? ResolveSubtitle(ItunesResult item)
    {
        return string.Join(" - ", new[] { item.ArtistName, ReadYear(item.ReleaseDate) }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? ResolveDescription(ItunesResult item)
    {
        return NormalizeOptional(item.LongDescription ?? item.Description ?? item.ShortDescription ?? item.Copyright);
    }

    private static int? ResolveRuntimeMinutes(ItunesResult item)
    {
        return item.TrackTimeMillis is > 0 ? (int)Math.Round(item.TrackTimeMillis.Value / 60000d) : null;
    }

    private static string? NormalizeArtwork(string? artworkUrl)
    {
        return string.IsNullOrWhiteSpace(artworkUrl)
            ? null
            : artworkUrl.Replace("100x100bb", "600x600bb", StringComparison.OrdinalIgnoreCase);
    }

    private Dictionary<string, string> BuildMetadata(ItunesResult item)
    {
        return Metadata(
            ("artist", item.ArtistName),
            ("collectionName", item.CollectionName),
            ("genre", item.PrimaryGenreName),
            ("year", ReadYear(item.ReleaseDate)),
            ("collectionType", item.CollectionType),
            ("trackCount", item.TrackCount?.ToString()),
            ("country", item.Country),
            ("currency", item.Currency),
            ("storeKind", item.Kind),
            ("contentAdvisoryRating", item.ContentAdvisoryRating));
    }

    private bool IsResultAllowed(ItunesResult item)
    {
        if (SupportedKind == ExternalMetadataKinds.Single)
        {
            return IsSingle(item);
        }

        if (SupportedKind != ExternalMetadataKinds.Album)
        {
            return true;
        }

        return IsAlbum(item);
    }

    private bool IsAlbum(ItunesResult item)
    {
        var title = ResolveTitle(item);
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        if (string.Equals(item.CollectionType, "Single", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (item.TrackCount is <= 1)
        {
            return false;
        }

        return !LooksLikeSingleOrEp(title);
    }

    private bool IsSingle(ItunesResult item)
    {
        var title = ResolveTitle(item);
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        return string.Equals(item.CollectionType, "Single", StringComparison.OrdinalIgnoreCase) ||
            item.TrackCount is <= 1 ||
            LooksLikeSingleOrEp(title);
    }

    private static bool LooksLikeSingleOrEp(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        return normalized.EndsWith(" - single", StringComparison.Ordinal) ||
            normalized.EndsWith(" - ep", StringComparison.Ordinal) ||
            normalized.Contains("(single)", StringComparison.Ordinal) ||
            normalized.Contains("(ep)", StringComparison.Ordinal);
    }

    private static string? ReadYear(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Length < 4 ? null : value[..4];
    }

    private sealed class ItunesSearchResponse
    {
        public List<ItunesResult> Results { get; set; } = [];
    }

    private sealed class ItunesResult
    {
        public long? TrackId { get; set; }
        public long? CollectionId { get; set; }
        public string? TrackName { get; set; }
        public string? CollectionName { get; set; }
        public string? ArtistName { get; set; }
        public string? LongDescription { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Copyright { get; set; }
        public string? ArtworkUrl100 { get; set; }
        public string? ReleaseDate { get; set; }
        public string? TrackViewUrl { get; set; }
        public string? CollectionViewUrl { get; set; }
        public string? PrimaryGenreName { get; set; }
        public string? Country { get; set; }
        public string? Currency { get; set; }
        public string? Kind { get; set; }
        public string? CollectionType { get; set; }
        public string? ContentAdvisoryRating { get; set; }
        public int? TrackTimeMillis { get; set; }
        public int? TrackCount { get; set; }
        public double? AverageUserRating { get; set; }
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = [];
    }
}
