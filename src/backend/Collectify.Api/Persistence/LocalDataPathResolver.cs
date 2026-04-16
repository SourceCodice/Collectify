using Microsoft.Extensions.Options;

namespace Collectify.Api.Persistence;

public sealed class LocalDataPathResolver(IOptions<LocalDataOptions> options, IHostEnvironment environment)
{
    public LocalDataPaths Resolve()
    {
        var configuredRoot = Environment.ExpandEnvironmentVariables(options.Value.RootPath);
        var rootPath = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));

        return new LocalDataPaths(
            rootPath,
            Path.Combine(rootPath, options.Value.DataFileName),
            Path.Combine(rootPath, options.Value.ImagesDirectoryName));
    }
}
