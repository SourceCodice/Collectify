using Collectify.Api.Modules.Collections;
using Collectify.Api.Persistence;

namespace Collectify.Api.Modules.Search;

public sealed class LocalSearchApplicationService(ICollectifyDataStore dataStore)
{
    private static readonly HashSet<string> RatingKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "rating",
        "voteAverage",
        "vote_average",
        "metacritic",
        "valutazione",
        "voto"
    };

    public async Task<LocalSearchResponse> SearchItemsAsync(LocalSearchQuery query, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var categoriesById = document.Categories.ToDictionary(category => category.Id);
        var tagsById = document.Tags.ToDictionary(tag => tag.Id);

        var indexedItems = document.Collections
            .SelectMany(collection => collection.Items.Select(item => BuildIndexedItem(collection, item, categoriesById, tagsById)))
            .ToList();

        var filteredItems = indexedItems
            .Where(item => MatchesText(item, query.Query))
            .Where(item => !query.CollectionId.HasValue || item.Collection.Id == query.CollectionId.Value)
            .Where(item => !query.CategoryId.HasValue || item.Collection.CategoryId == query.CategoryId.Value)
            .Where(item => !query.TagId.HasValue || item.Item.TagIds.Contains(query.TagId.Value))
            .Where(item => string.IsNullOrWhiteSpace(query.Condition) || string.Equals(item.Item.Condition, query.Condition, StringComparison.OrdinalIgnoreCase))
            .Where(item => !query.MinRating.HasValue || (item.Rating.HasValue && item.Rating.Value >= query.MinRating.Value))
            .Where(item => !query.MaxRating.HasValue || (item.Rating.HasValue && item.Rating.Value <= query.MaxRating.Value))
            .Where(item => IsInsideDateRange(item, query))
            .ToList();

        var orderedItems = ApplySort(filteredItems, query).ToList();

        return new LocalSearchResponse(
            orderedItems.Count,
            orderedItems.Select(item => item.ToResponse()).ToList(),
            BuildFacets(indexedItems));
    }

    private static IndexedSearchItem BuildIndexedItem(
        Collection collection,
        Item item,
        IReadOnlyDictionary<Guid, CollectionCategory> categoriesById,
        IReadOnlyDictionary<Guid, Tag> tagsById)
    {
        var category = collection.CategoryId.HasValue && categoriesById.TryGetValue(collection.CategoryId.Value, out var foundCategory)
            ? foundCategory
            : null;
        var tags = item.TagIds
            .Where(tagsById.ContainsKey)
            .Select(tagId => tagsById[tagId])
            .ToList();
        var rating = ResolveRating(item);

        return new IndexedSearchItem(collection, item, category, tags, rating);
    }

    private static bool MatchesText(IndexedSearchItem item, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token => item.SearchFields.Any(field => field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsInsideDateRange(IndexedSearchItem item, LocalSearchQuery query)
    {
        if (!query.DateFrom.HasValue && !query.DateTo.HasValue)
        {
            return true;
        }

        var date = query.DateField.ToLowerInvariant() switch
        {
            "createdat" or "created" => item.Item.CreatedAt,
            "acquiredat" or "acquired" => item.Item.AcquiredAt,
            _ => item.Item.UpdatedAt
        };

        if (!date.HasValue)
        {
            return false;
        }

        if (query.DateFrom.HasValue && date.Value.Date < query.DateFrom.Value.Date)
        {
            return false;
        }

        if (query.DateTo.HasValue && date.Value.Date > query.DateTo.Value.Date)
        {
            return false;
        }

        return true;
    }

    private static IOrderedEnumerable<IndexedSearchItem> ApplySort(IReadOnlyList<IndexedSearchItem> items, LocalSearchQuery query)
    {
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return query.SortBy.ToLowerInvariant() switch
        {
            "createdat" or "created" => descending
                ? items.OrderByDescending(item => item.Item.CreatedAt)
                : items.OrderBy(item => item.Item.CreatedAt),
            "updatedat" or "updated" => descending
                ? items.OrderByDescending(item => item.Item.UpdatedAt)
                : items.OrderBy(item => item.Item.UpdatedAt),
            "value" => descending
                ? items.OrderByDescending(item => item.Item.EstimatedValue ?? decimal.MinValue)
                : items.OrderBy(item => item.Item.EstimatedValue ?? decimal.MaxValue),
            _ => descending
                ? items.OrderByDescending(item => item.Item.Title)
                : items.OrderBy(item => item.Item.Title)
        };
    }

    private static LocalSearchFacetsResponse BuildFacets(IReadOnlyList<IndexedSearchItem> items)
    {
        var categories = items
            .Where(item => item.Category is not null)
            .GroupBy(item => item.Category!)
            .OrderBy(group => group.Key.SortOrder)
            .ThenBy(group => group.Key.Name)
            .Select(group => new SearchOptionResponse(group.Key.Id.ToString(), group.Key.Name, group.Count()))
            .ToList();

        var tags = items
            .SelectMany(item => item.Tags)
            .GroupBy(tag => tag.Id)
            .Select(group => new SearchOptionResponse(group.Key.ToString(), group.First().Name, group.Count()))
            .OrderBy(option => option.Label)
            .ToList();

        var conditions = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Item.Condition))
            .GroupBy(item => item.Item.Condition, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SearchOptionResponse(group.Key, group.Key, group.Count()))
            .OrderBy(option => option.Label)
            .ToList();

        return new LocalSearchFacetsResponse(
            categories,
            tags,
            conditions,
            BuildRange(items.Select(item => item.Rating)),
            BuildRange(items.Select(item => item.Item.EstimatedValue)));
    }

    private static SearchRangeResponse? BuildRange(IEnumerable<decimal?> values)
    {
        var concreteValues = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return concreteValues.Count == 0
            ? null
            : new SearchRangeResponse(concreteValues.Min(), concreteValues.Max());
    }

    private static decimal? ResolveRating(Item item)
    {
        foreach (var attribute in item.Attributes)
        {
            if (RatingKeys.Contains(attribute.Key) || RatingKeys.Contains(attribute.Label))
            {
                var normalizedValue = attribute.Value.Replace(',', '.');
                if (decimal.TryParse(normalizedValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var rating))
                {
                    return rating;
                }
            }
        }

        return null;
    }

    private sealed record IndexedSearchItem(
        Collection Collection,
        Item Item,
        CollectionCategory? Category,
        IReadOnlyList<Tag> Tags,
        decimal? Rating)
    {
        public IEnumerable<string> SearchFields
        {
            get
            {
                yield return Collection.Name;
                yield return Collection.Description ?? string.Empty;
                yield return Item.Title;
                yield return Item.Description ?? string.Empty;
                yield return Item.Notes ?? string.Empty;
                yield return Item.Condition;

                foreach (var tag in Tags)
                {
                    yield return tag.Name;
                }

                foreach (var attribute in Item.Attributes)
                {
                    yield return attribute.Key;
                    yield return attribute.Label;
                    yield return attribute.Value;
                }
            }
        }

        public LocalSearchResultResponse ToResponse()
        {
            return new LocalSearchResultResponse(
                Collection.Id,
                Collection.Name,
                Collection.Type,
                Collection.CategoryId,
                Category?.Name,
                Item.ToResponse(),
                Tags.Select(tag => new SearchOptionResponse(tag.Id.ToString(), tag.Name, 1)).ToList(),
                Rating,
                Item.EstimatedValue);
        }
    }
}

public sealed record LocalSearchQuery(
    string? Query,
    Guid? CollectionId,
    Guid? CategoryId,
    Guid? TagId,
    string? Condition,
    decimal? MinRating,
    decimal? MaxRating,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string DateField,
    string SortBy,
    string SortDirection);
