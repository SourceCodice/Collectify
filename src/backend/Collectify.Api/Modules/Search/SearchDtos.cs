using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Modules.Search;

public sealed record LocalSearchResponse(
    int TotalCount,
    IReadOnlyList<LocalSearchResultResponse> Items,
    LocalSearchFacetsResponse Facets);

public sealed record LocalSearchResultResponse(
    Guid CollectionId,
    string CollectionName,
    string CollectionType,
    Guid? CategoryId,
    string? CategoryName,
    CollectionItemResponse Item,
    IReadOnlyList<SearchOptionResponse> Tags,
    decimal? Rating,
    decimal? Value);

public sealed record LocalSearchFacetsResponse(
    IReadOnlyList<SearchOptionResponse> Categories,
    IReadOnlyList<SearchOptionResponse> Tags,
    IReadOnlyList<SearchOptionResponse> Conditions,
    SearchRangeResponse? RatingRange,
    SearchRangeResponse? ValueRange);

public sealed record SearchOptionResponse(string Id, string Label, int Count);

public sealed record SearchRangeResponse(decimal Min, decimal Max);
