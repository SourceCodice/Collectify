namespace Collectify.Api.Modules.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/search")
            .WithTags("Local search");

        group.MapGet("/items", async (
            string? query,
            Guid? collectionId,
            Guid? categoryId,
            Guid? tagId,
            string? condition,
            decimal? minRating,
            decimal? maxRating,
            DateTimeOffset? dateFrom,
            DateTimeOffset? dateTo,
            string? dateField,
            string? sortBy,
            string? sortDirection,
            LocalSearchApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var searchQuery = new LocalSearchQuery(
                query,
                collectionId,
                categoryId,
                tagId,
                condition,
                minRating,
                maxRating,
                dateFrom,
                dateTo,
                string.IsNullOrWhiteSpace(dateField) ? "updatedAt" : dateField,
                string.IsNullOrWhiteSpace(sortBy) ? "updatedAt" : sortBy,
                string.IsNullOrWhiteSpace(sortDirection) ? "desc" : sortDirection);

            return Results.Ok(await service.SearchItemsAsync(searchQuery, cancellationToken));
        });

        return endpoints;
    }
}
