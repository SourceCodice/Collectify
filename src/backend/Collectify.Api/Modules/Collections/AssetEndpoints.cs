using Collectify.Api.Persistence;

namespace Collectify.Api.Modules.Collections;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/assets/{**relativePath}", (string relativePath, LocalDataPathResolver pathResolver) =>
        {
            var paths = pathResolver.Resolve();
            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.GetFullPath(Path.Combine(paths.RootPath, normalized));
            var imagesRoot = Path.GetFullPath(paths.ImagesPath);

            if (!physicalPath.StartsWith(imagesRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(physicalPath))
            {
                return Results.NotFound();
            }

            return Results.File(physicalPath, GetContentType(physicalPath));
        });

        return endpoints;
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
