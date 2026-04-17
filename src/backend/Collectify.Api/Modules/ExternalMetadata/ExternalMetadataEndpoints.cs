namespace Collectify.Api.Modules.ExternalMetadata;

public static class ExternalMetadataEndpoints
{
    public static IEndpointRouteBuilder MapExternalMetadataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/external")
            .WithTags("External metadata");

        group.MapGet("/providers", (string? itemType, string? macroCategory, ExternalMetadataApplicationService service) =>
        {
            var category = ResolveRequestedCategory(itemType, macroCategory);
            if (string.IsNullOrWhiteSpace(category))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["macroCategory"] = ["Item type or macro category is required."]
                });
            }

            return Results.Ok(service.ResolveProviders(category));
        });

        group.MapGet("/search", (string query, string? itemType, string? macroCategory, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => SearchByCategoryAsync(ResolveRequestedCategory(itemType, macroCategory), query, service, cancellationToken)));

        group.MapGet("/live/search", (string query, string? itemType, string? macroCategory, string? provider, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => SearchLiveAsync(ResolveRequestedCategory(itemType, macroCategory), query, provider, service, cancellationToken)));

        group.MapGet("/live/details", (string provider, string externalId, string? itemType, string? macroCategory, ExternalMetadataApplicationService service, CancellationToken cancellationToken) =>
            ExecuteAsync(() => LiveDetailsAsync(ResolveRequestedCategory(itemType, macroCategory), provider, externalId, service, cancellationToken)));

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

    private static async Task<IResult> SearchByCategoryAsync(
        string? category,
        string query,
        ExternalMetadataApplicationService service,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(category))
        {
            errors["macroCategory"] = ["Item type or macro category is required."];
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            errors["query"] = ["Search query is required."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var results = await service.SearchByCategoryAsync(category!, query, cancellationToken);
        return Results.Ok(results);
    }

    private static async Task<IResult> SearchLiveAsync(
        string? category,
        string query,
        string? provider,
        ExternalMetadataApplicationService service,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(category))
        {
            errors["macroCategory"] = ["Item type or macro category is required."];
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            errors["query"] = ["Search query is required."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        return Results.Ok(await service.SearchLiveAsync(category!, query, provider, cancellationToken));
    }

    private static async Task<IResult> LiveDetailsAsync(
        string? category,
        string provider,
        string externalId,
        ExternalMetadataApplicationService service,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(category))
        {
            errors["macroCategory"] = ["Item type or macro category is required."];
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            errors["provider"] = ["Provider is required."];
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            errors["externalId"] = ["External id is required."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var details = await service.GetLiveDetailsAsync(category!, provider, externalId, cancellationToken);
        return details is null ? Results.NotFound() : Results.Ok(details);
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

    private static string? ResolveRequestedCategory(string? itemType, string? macroCategory)
    {
        return string.IsNullOrWhiteSpace(macroCategory) ? itemType : macroCategory;
    }
}
