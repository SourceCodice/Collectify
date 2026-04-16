namespace Collectify.Api.Modules.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/settings")
            .WithTags("Settings");

        group.MapGet("/", async (AppSettingsApplicationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(cancellationToken)));

        group.MapPut("/", async (UpdateAppSettingsRequest request, AppSettingsApplicationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(request, cancellationToken);
            return result.IsValid
                ? Results.Ok(result.Value)
                : Results.ValidationProblem(result.Errors);
        });

        return endpoints;
    }
}
