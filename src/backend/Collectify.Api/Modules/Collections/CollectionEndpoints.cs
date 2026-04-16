namespace Collectify.Api.Modules.Collections;

public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/collections")
            .WithTags("Collections");

        group.MapGet("/", async (ICollectionRepository repository, CancellationToken cancellationToken) =>
        {
            var collections = await repository.ListAsync(cancellationToken);
            return Results.Ok(collections.Select(collection => collection.ToSummaryResponse()));
        });

        group.MapGet("/{id:guid}", async (Guid id, ICollectionRepository repository, CancellationToken cancellationToken) =>
        {
            var collection = await repository.GetAsync(id, cancellationToken);
            return collection is null
                ? Results.NotFound()
                : Results.Ok(collection.ToDetailResponse());
        });

        group.MapPost("/", async (CreateCollectionRequest request, ICollectionRepository repository, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
            {
                return Results.BadRequest(new { message = "Name and type are required." });
            }

            var collection = await repository.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/collections/{collection.Id}", collection.ToDetailResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCollectionRequest request, ICollectionRepository repository, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
            {
                return Results.BadRequest(new { message = "Name and type are required." });
            }

            var collection = await repository.UpdateAsync(id, request, cancellationToken);
            return collection is null
                ? Results.NotFound()
                : Results.Ok(collection.ToDetailResponse());
        });

        group.MapDelete("/{id:guid}", async (Guid id, ICollectionRepository repository, CancellationToken cancellationToken) =>
        {
            var deleted = await repository.DeleteAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/items", async (Guid id, AddCollectionItemRequest request, ICollectionRepository repository, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { message = "Title is required." });
            }

            var item = await repository.AddItemAsync(id, request, cancellationToken);
            return item is null
                ? Results.NotFound()
                : Results.Created($"/api/collections/{id}/items/{item.Id}", item.ToResponse());
        });

        group.MapDelete("/{collectionId:guid}/items/{itemId:guid}", async (Guid collectionId, Guid itemId, ICollectionRepository repository, CancellationToken cancellationToken) =>
        {
            var deleted = await repository.DeleteItemAsync(collectionId, itemId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return endpoints;
    }
}
