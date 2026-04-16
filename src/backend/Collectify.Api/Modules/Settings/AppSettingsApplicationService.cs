using Collectify.Api.Modules.Collections;
using Collectify.Api.Persistence;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.Settings;

public sealed class AppSettingsApplicationService(AppSettingsFileStore settingsStore, IOptions<LocalDataOptions> options)
{
    public async Task<AppSettingsResponse> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);
        var paths = EnsureApplicationDirectories(settings);
        return ToResponse(settings, paths);
    }

    public async Task<ValidationResult<AppSettingsResponse>> UpdateAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return ValidationResult<AppSettingsResponse>.Failure(errors);
        }

        var settings = await settingsStore.GetAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.DataRootPath))
        {
            settings.DataRootPath = request.DataRootPath.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Theme))
        {
            settings.Theme = request.Theme.Trim();
        }

        if (request.AutomaticBackupEnabled.HasValue)
        {
            settings.AutomaticBackupEnabled = request.AutomaticBackupEnabled.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            settings.Language = request.Language.Trim();
            settings.Locale = request.Language.Trim();
        }

        settings = await settingsStore.SaveAsync(settings, cancellationToken);
        var paths = EnsureApplicationDirectories(settings);

        return ValidationResult<AppSettingsResponse>.Success(ToResponse(settings, paths));
    }

    public LocalDataPaths EnsureApplicationDirectories(AppSettings settings)
    {
        var rootPath = settingsStore.ResolveDataRootPath(settings);
        var paths = new LocalDataPaths(
            rootPath,
            Path.Combine(rootPath, options.Value.DataFileName),
            Path.Combine(rootPath, options.Value.ImagesDirectoryName),
            Path.Combine(rootPath, options.Value.BackupsDirectoryName),
            settingsStore.ResolveSettingsFilePath(),
            settings.AutomaticBackupEnabled);

        Directory.CreateDirectory(paths.RootPath);
        Directory.CreateDirectory(paths.ImagesPath);

        if (paths.AutomaticBackupEnabled)
        {
            Directory.CreateDirectory(paths.BackupsPath);
        }

        return paths;
    }

    private AppSettingsResponse ToResponse(AppSettings settings, LocalDataPaths paths)
    {
        return new AppSettingsResponse(
            paths.RootPath,
            paths.DataFilePath,
            paths.ImagesPath,
            paths.BackupsPath,
            paths.SettingsFilePath,
            settings.Theme,
            settings.AutomaticBackupEnabled,
            settings.Language,
            settings.Locale,
            settings.CreatedAt,
            settings.UpdatedAt);
    }

    private static Dictionary<string, string[]> Validate(UpdateAppSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Theme is not null && string.IsNullOrWhiteSpace(request.Theme))
        {
            errors[nameof(UpdateAppSettingsRequest.Theme)] = ["Theme cannot be empty."];
        }

        if (request.Language is not null && string.IsNullOrWhiteSpace(request.Language))
        {
            errors[nameof(UpdateAppSettingsRequest.Language)] = ["Language cannot be empty."];
        }

        if (request.DataRootPath is not null && string.IsNullOrWhiteSpace(request.DataRootPath))
        {
            errors[nameof(UpdateAppSettingsRequest.DataRootPath)] = ["Data folder path cannot be empty."];
        }

        return errors;
    }
}
