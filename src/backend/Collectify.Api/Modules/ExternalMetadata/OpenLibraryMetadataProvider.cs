using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class OpenLibraryMetadataProvider(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
    : ExternalMetadataProviderBase(httpClientFactory, options), IExternalMetadataProvider
{
    public string ProviderId => "openlibrary";
    public string SupportedKind => ExternalMetadataKinds.Book;

    public async Task<IReadOnlyList<ExternalMetadataSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var response = await GetJsonAsync<OpenLibrarySearchResponse>(
            BuildUrl(
                Options.OpenLibrary.BaseUrl,
                "search.json",
                ("q", query),
                ("limit", "12"),
                ("fields", "key,title,author_name,first_publish_year,isbn,cover_i,subject")),
            Options.OpenLibrary,
            configureHeaders: null,
            cancellationToken);

        return response?.Docs
            .Where(book => !string.IsNullOrWhiteSpace(book.Key) && !string.IsNullOrWhiteSpace(book.Title))
            .Select(book => new ExternalMetadataSearchResult(
                ProviderId,
                SupportedKind,
                Normalize(book.Key),
                Normalize(book.Title),
                book.FirstPublishYear?.ToString(),
                string.Join(", ", book.AuthorName.Take(4)),
                BuildCoverUrl(book.CoverId),
                book.FirstPublishYear?.ToString(),
                $"https://openlibrary.org{book.Key}",
                Metadata(
                    ("authors", string.Join(", ", book.AuthorName.Take(8))),
                    ("isbn", book.Isbn.FirstOrDefault()),
                    ("subjects", string.Join(", ", book.Subject.Take(8))))))
            .ToList() ?? [];
    }

    public async Task<ExternalMetadataDetails?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        var workKey = NormalizeWorkKey(externalId);
        var response = await GetJsonAsync<OpenLibraryWorkDetails>(
            BuildUrl(Options.OpenLibrary.BaseUrl, $"{workKey}.json"),
            Options.OpenLibrary,
            configureHeaders: null,
            cancellationToken);

        if (response is null || string.IsNullOrWhiteSpace(response.Title))
        {
            return null;
        }

        var coverId = response.Covers.FirstOrDefault();
        var attributes = new List<ExternalMetadataAttribute>
        {
            new("releaseDate", "Prima pubblicazione", Normalize(response.FirstPublishDate), "Date"),
            new("subjects", "Soggetti", string.Join(", ", response.Subjects.Take(12))),
            new("places", "Luoghi", string.Join(", ", response.SubjectPlaces.Take(8))),
            new("times", "Periodi", string.Join(", ", response.SubjectTimes.Take(8)))
        }.Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value)).ToList();

        return new ExternalMetadataDetails(
            ProviderId,
            SupportedKind,
            $"/{workKey}",
            Normalize(response.Title),
            ReadDescription(response.Description),
            BuildCoverUrl(coverId),
            response.FirstPublishDate,
            $"https://openlibrary.org/{workKey}",
            attributes,
            Metadata(("coverId", coverId > 0 ? coverId.ToString() : null)));
    }

    private static string NormalizeWorkKey(string externalId)
    {
        var trimmed = externalId.Trim().Trim('/');
        return trimmed.StartsWith("works/", StringComparison.OrdinalIgnoreCase) ? trimmed : $"works/{trimmed}";
    }

    private static string? BuildCoverUrl(int? coverId)
    {
        return coverId is null or <= 0 ? null : $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg";
    }

    private static string? ReadDescription(JsonElement? description)
    {
        if (description is null)
        {
            return null;
        }

        return description.Value.ValueKind switch
        {
            JsonValueKind.String => NormalizeOptional(description.Value.GetString()),
            JsonValueKind.Object when description.Value.TryGetProperty("value", out var value) => NormalizeOptional(value.GetString()),
            _ => null
        };
    }

    private sealed class OpenLibrarySearchResponse
    {
        public List<OpenLibraryBookSummary> Docs { get; set; } = [];
    }

    private sealed class OpenLibraryBookSummary
    {
        public string? Key { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("author_name")]
        public List<string> AuthorName { get; set; } = [];
        [JsonPropertyName("first_publish_year")]
        public int? FirstPublishYear { get; set; }
        public List<string> Isbn { get; set; } = [];
        [JsonPropertyName("cover_i")]
        public int? CoverId { get; set; }
        public List<string> Subject { get; set; } = [];
    }

    private sealed class OpenLibraryWorkDetails
    {
        public string? Title { get; set; }
        public JsonElement? Description { get; set; }
        [JsonPropertyName("first_publish_date")]
        public string? FirstPublishDate { get; set; }
        public List<int> Covers { get; set; } = [];
        public List<string> Subjects { get; set; } = [];
        [JsonPropertyName("subject_places")]
        public List<string> SubjectPlaces { get; set; } = [];
        [JsonPropertyName("subject_times")]
        public List<string> SubjectTimes { get; set; } = [];
    }
}
