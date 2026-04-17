using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class TmdbMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public string ProviderId => "tmdb";
    public string SupportedKind => ExternalMetadataKinds.Movie;

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var response = await GetJsonAsync<TmdbSearchResponse>(
            BuildUrl(
                Options.TMDb.BaseUrl,
                "search/movie",
                ("query", query),
                ("language", "it-IT"),
                ("include_adult", "false"),
                ("api_key", ApiKeyOrNull())),
            Options.TMDb,
            ConfigureHeaders,
            cancellationToken);

        return response?.Results
            .Where(movie => movie.Id > 0 && !string.IsNullOrWhiteSpace(movie.Title))
            .Select(movie => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                movie.Id.ToString(),
                Normalize(movie.Title),
                movie.ReleaseDate,
                NormalizeOptional(movie.Overview),
                BuildPosterUrl(movie.PosterPath),
                movie.ReleaseDate,
                $"https://www.themoviedb.org/movie/{movie.Id}",
                Metadata(
                    ("originalTitle", movie.OriginalTitle),
                    ("voteAverage", movie.VoteAverage?.ToString("0.0")),
                    ("backdropUrl", BuildBackdropUrl(movie.BackdropPath)))))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var response = await GetJsonAsync<TmdbMovieDetails>(
            BuildUrl(
                Options.TMDb.BaseUrl,
                $"movie/{externalId}",
                ("language", "it-IT"),
                ("api_key", ApiKeyOrNull())),
            Options.TMDb,
            ConfigureHeaders,
            cancellationToken);

        if (response is null || response.Id == 0 || string.IsNullOrWhiteSpace(response.Title))
        {
            return null;
        }

        var attributes = new List<ExternalMetadataAttribute>
        {
            new("releaseDate", "Data uscita", Normalize(response.ReleaseDate), "Date"),
            new("runtime", "Durata", response.Runtime?.ToString() ?? string.Empty, "Number", "min"),
            new("genres", "Generi", string.Join(", ", response.Genres.Select(genre => genre.Name))),
            new("status", "Stato", Normalize(response.Status)),
            new("language", "Lingua originale", Normalize(response.OriginalLanguage)),
            new("rating", "Valutazione TMDb", response.VoteAverage?.ToString("0.0") ?? string.Empty, "Number")
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            response.Id.ToString(),
            Normalize(response.Title),
            NormalizeOptional(response.Overview),
            BuildPosterUrl(response.PosterPath),
            response.ReleaseDate,
            $"https://www.themoviedb.org/movie/{response.Id}",
            attributes,
            Metadata(
                ("imdbId", response.ImdbId),
                ("homepage", response.Homepage),
                ("originalTitle", response.OriginalTitle),
                ("backdropUrl", BuildBackdropUrl(response.BackdropPath))));
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(Options.TMDb.ApiKey) && string.IsNullOrWhiteSpace(Options.TMDb.AccessToken))
        {
            throw new ExternalMetadataProviderException("TMDb API key is missing.", StatusCodes.Status503ServiceUnavailable);
        }
    }

    private string? ApiKeyOrNull()
    {
        return string.IsNullOrWhiteSpace(Options.TMDb.ApiKey) ? null : Options.TMDb.ApiKey;
    }

    private void ConfigureHeaders(HttpRequestHeaders headers)
    {
        if (!string.IsNullOrWhiteSpace(Options.TMDb.AccessToken))
        {
            headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.TMDb.AccessToken);
        }
    }

    private static string? BuildPosterUrl(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : $"https://image.tmdb.org/t/p/w500{path}";
    }

    private static string? BuildBackdropUrl(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : $"https://image.tmdb.org/t/p/w1280{path}";
    }

    private sealed class TmdbSearchResponse
    {
        public List<TmdbMovieSummary> Results { get; set; } = [];
    }

    private class TmdbMovieSummary
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonPropertyName("vote_average")]
        public double? VoteAverage { get; set; }
    }

    private sealed class TmdbMovieDetails : TmdbMovieSummary
    {
        public string? Homepage { get; set; }
        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }
        public int? Runtime { get; set; }
        public string? Status { get; set; }
        public List<TmdbGenre> Genres { get; set; } = [];
    }

    private sealed class TmdbGenre
    {
        public string Name { get; set; } = string.Empty;
    }
}
