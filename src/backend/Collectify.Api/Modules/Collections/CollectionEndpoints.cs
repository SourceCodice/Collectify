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

        group.MapPost("/{collectionId:guid}/items/{itemId:guid}/images", async (Guid collectionId, Guid itemId, HttpRequest request, ItemImageApplicationService service, CancellationToken cancellationToken) =>
        {
            var formResult = await ReadImageFormAsync(request, cancellationToken);
            if (!formResult.IsValid)
            {
                return Results.ValidationProblem(formResult.Errors);
            }

            var result = await service.AddImageAsync(
                collectionId,
                itemId,
                formResult.Value!.File,
                formResult.Value.Caption,
                formResult.Value.IsPrimary ?? false,
                cancellationToken);

            if (!result.IsValid)
            {
                return Results.ValidationProblem(result.Errors);
            }

            return result.Value is null
                ? Results.NotFound()
                : Results.Created($"/api/collections/{collectionId}/items/{itemId}/images/{result.Value.Id}", result.Value);
        });

        group.MapPut("/{collectionId:guid}/items/{itemId:guid}/images/{imageId:guid}", async (Guid collectionId, Guid itemId, Guid imageId, HttpRequest request, ItemImageApplicationService service, CancellationToken cancellationToken) =>
        {
            var formResult = await ReadImageFormAsync(request, cancellationToken);
            if (!formResult.IsValid)
            {
                return Results.ValidationProblem(formResult.Errors);
            }

            var result = await service.ReplaceImageAsync(
                collectionId,
                itemId,
                imageId,
                formResult.Value!.File,
                formResult.Value.Caption,
                formResult.Value.IsPrimary,
                cancellationToken);

            if (!result.IsValid)
            {
                return Results.ValidationProblem(result.Errors);
            }

            return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
        });

        group.MapDelete("/{collectionId:guid}/items/{itemId:guid}/images/{imageId:guid}", async (Guid collectionId, Guid itemId, Guid imageId, ItemImageApplicationService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteImageAsync(collectionId, itemId, imageId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{collectionId:guid}/items/{itemId:guid}", async (Guid collectionId, Guid itemId, CollectionApplicationService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteItemAsync(collectionId, itemId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return endpoints;
    }

    private static async Task<ValidationResult<ImageUploadForm>> ReadImageFormAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return ValidationResult<ImageUploadForm>.Failure(new Dictionary<string, string[]>
            {
                ["contentType"] = ["multipart/form-data content is required."]
            });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");

        if (file is null)
        {
            return ValidationResult<ImageUploadForm>.Failure(new Dictionary<string, string[]>
            {
                ["file"] = ["Image file is required."]
            });
        }

        bool? isPrimary = null;
        if (bool.TryParse(form["isPrimary"], out var parsedIsPrimary))
        {
            isPrimary = parsedIsPrimary;
        }

        return ValidationResult<ImageUploadForm>.Success(new ImageUploadForm(
            file,
            form["caption"].FirstOrDefault(),
            isPrimary));
    }

    private sealed record ImageUploadForm(IFormFile File, string? Caption, bool? IsPrimary);
}
