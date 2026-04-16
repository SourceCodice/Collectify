namespace Collectify.Api.Modules.ExternalMetadata;

public static class ExternalMetadataEndpoints
{
    public static IEndpointRouteBuilder MapExternalMetadataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/external")
            .WithTags("External metadata");

        group.MapGet("/movies/search", (string query, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => SearchAsync(ExternalMetadataKinds.Movie, query, service, cancellationToken)));

        group.MapGet("/movies/{externalId}", (string externalId, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => DetailsAsync("tmdb", externalId, service, cancellationToken)));

        group.MapGet("/games/search", (string query, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => SearchAsync(ExternalMetadataKinds.Game, query, service, cancellationToken)));

        group.MapGet("/games/{externalId}", (string externalId, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => DetailsAsync("rawg", externalId, service, cancellationToken)));

        group.MapGet("/albums/search", (string query, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => SearchAsync(ExternalMetadataKinds.Album, query, service, cancellationToken)));

        group.MapGet("/albums/{externalId}", (string externalId, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => DetailsAsync("discogs", externalId, service, cancellationToken)));

        group.MapPost("/import", (ImportExternalItemRequest request, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(async () =>
            {
                var result = await service.ImportAsync(request, cancellationToken);

                if (!result.IsValid)
                {
                    return Results.ValidationProblem(result.Errors);
                }

                return result.Value is null ? Results.NotFound() : Results.Created($"/api/collections/{request.CollectionId}/items/{result.Value.Id}", result.Value);
            }));

        return endpoints;
    }

    private static async Task<IResult> SearchAsync(string kind, string query, ExternalMetadataApplicationService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["query"] = ["Search query is required."]
            });
        }

        var results = await service.SearchAsync(kind, query, cancellationToken);
        return Results.Ok(results);
    }

    private static async Task<IResult> DetailsAsync(string provider, string externalId, ExternalMetadataApplicationService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["externalId"] = ["External id is required."]
            });
        }

        var details = await service.GetDetailsAsync(provider, externalId, cancellationToken);
        return details is null ? Results.NotFound() : Results.Ok(details);
    }

    private static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ExternalMetadataProviderException exception)
        {
            return Results.Problem(
                title: "External metadata unavailable",
                detail: exception.Message,
                statusCode: exception.StatusCode);
        }
    }
}
