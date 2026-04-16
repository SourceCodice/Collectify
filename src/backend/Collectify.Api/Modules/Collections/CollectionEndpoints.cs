namespace Collectify.Api.Modules.Collections;

public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/collections")
            .WithTags("Collections");

        group.MapGet("/", async (CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var collections = await service.ListCollectionsAsync(cancellationToken);
            return Results.Ok(collections);
        });

        group.MapGet("/{id:guid}", async (Guid id, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var collection = await service.GetCollectionAsync(id, cancellationToken);
            return collection is null
                ? Results.NotFound()
                : Results.Ok(collection);
        });

        group.MapGet("/{id:guid}/items", async (Guid id, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var items = await service.ListItemsAsync(id, cancellationToken);
            return items is null ? Results.NotFound() : Results.Ok(items);
        });

        group.MapGet("/{collectionId:guid}/items/{itemId:guid}", async (Guid collectionId, Guid itemId, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var item = await service.GetItemAsync(collectionId, itemId, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async (CreateCollectionRequest request, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.CreateCollectionAsync(request, cancellationToken);
            return result.IsValid
                ? Results.Created($"/api/collections/{result.Value!.Id}", result.Value)
                : Results.ValidationProblem(result.Errors);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCollectionRequest request, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateCollectionAsync(id, request, cancellationToken);

            if (!result.IsValid)
            {
                return Results.ValidationProblem(result.Errors);
            }

            return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
        });

        group.MapDelete("/{id:guid}", async (Guid id, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteCollectionAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/items", async (Guid id, AddCollectionItemRequest request, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.AddItemAsync(id, request, cancellationToken);

            if (!result.IsValid)
            {
                return Results.ValidationProblem(result.Errors);
            }

            return result.Value is null
                ? Results.NotFound()
                : Results.Created($"/api/collections/{id}/items/{result.Value.Id}", result.Value);
        });

        group.MapDelete("/{collectionId:guid}/items/{itemId:guid}", async (Guid collectionId, Guid itemId, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteItemAsync(collectionId, itemId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return endpoints;
    }
}
