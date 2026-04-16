using Collectify.Api.Modules.Settings;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Persistence;

public sealed class LocalDataPathResolver(IOptions<LocalDataOptions> options, AppSettingsFileStore settingsStore)
{
    public LocalDataPaths Resolve()
    {
        var settings = settingsStore.GetOrCreate();
        var rootPath = settingsStore.ResolveDataRootPath(settings);

        return new LocalDataPaths(
            rootPath,
            Path.Combine(rootPath, options.Value.DataFileName),
            Path.Combine(rootPath, options.Value.ImagesDirectoryName),
            Path.Combine(rootPath, options.Value.BackupsDirectoryName),
            settingsStore.ResolveSettingsFilePath(),
            settings.AutomaticBackupEnabled);
    }
}
