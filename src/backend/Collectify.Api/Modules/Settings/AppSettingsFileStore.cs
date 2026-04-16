using System.Text.Json;
using Collectify.Api.Modules.Collections;
using Collectify.Api.Persistence;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.Settings;

public sealed class AppSettingsFileStore(IOptions<LocalDataOptions> options, IHostEnvironment environment, ILogger<AppSettingsFileStore> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public string ResolveSettingsFilePath()
    {
        return Path.Combine(ResolveDefaultRootPath(), options.Value.SettingsFileName);
    }

    public AppSettings GetOrCreate()
    {
        var settingsPath = ResolveSettingsFilePath();
        var settings = ReadOrCreate(settingsPath);
        Normalize(settings);
        return settings;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return GetOrCreate();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            Normalize(settings);
            settings.UpdatedAt = DateTimeOffset.UtcNow;

            var settingsPath = ResolveSettingsFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            await WriteAtomicAsync(settingsPath, settings, cancellationToken);

            return settings;
        }
        finally
        {
            _lock.Release();
        }
    }

    public string ResolveDefaultRootPath()
    {
        var configuredRoot = Environment.ExpandEnvironmentVariables(options.Value.RootPath);
        return Path.IsPathRooted(configuredRoot)
            ? Path.GetFullPath(configuredRoot)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
    }

    public string ResolveDataRootPath(AppSettings settings)
    {
        var configuredRoot = string.IsNullOrWhiteSpace(settings.DataRootPath)
            ? ResolveDefaultRootPath()
            : Environment.ExpandEnvironmentVariables(settings.DataRootPath);

        return Path.IsPathRooted(configuredRoot)
            ? Path.GetFullPath(configuredRoot)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
    }

    private AppSettings ReadOrCreate(string settingsPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

        if (!File.Exists(settingsPath))
        {
            var settings = CreateDefaultSettings();
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
            return settings;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath), SerializerOptions);
            return settings ?? CreateDefaultSettings();
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            var corruptPath = $"{settingsPath}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(settingsPath, corruptPath);
            logger.LogWarning(exception, "Collectify settings file was corrupted and moved to {CorruptPath}.", corruptPath);

            var settings = CreateDefaultSettings();
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
            return settings;
        }
    }

    private AppSettings CreateDefaultSettings()
    {
        var now = DateTimeOffset.UtcNow;
        return new AppSettings
        {
            Id = Guid.NewGuid(),
            DataRootPath = ResolveDefaultRootPath(),
            Theme = "System",
            AutomaticBackupEnabled = true,
            Language = "it-IT",
            Locale = "it-IT",
            Currency = "EUR",
            DateFormat = "dd/MM/yyyy",
            DataSchemaVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private void Normalize(AppSettings settings)
    {
        var now = DateTimeOffset.UtcNow;
        settings.Id = settings.Id == Guid.Empty ? Guid.NewGuid() : settings.Id;
        settings.DataRootPath = ResolveDataRootPath(settings);
        settings.Theme = NormalizeValue(settings.Theme, "System");
        settings.Language = NormalizeValue(settings.Language, NormalizeValue(settings.Locale, "it-IT"));
        settings.Locale = NormalizeValue(settings.Locale, settings.Language);
        settings.Currency = NormalizeValue(settings.Currency, "EUR");
        settings.DateFormat = NormalizeValue(settings.DateFormat, "dd/MM/yyyy");
        settings.DataSchemaVersion = settings.DataSchemaVersion <= 0 ? 1 : settings.DataSchemaVersion;
        settings.CreatedAt = settings.CreatedAt == default ? now : settings.CreatedAt;
        settings.UpdatedAt = settings.UpdatedAt == default ? now : settings.UpdatedAt;
    }

    private async Task WriteAtomicAsync(string targetPath, AppSettings settings, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException($"Invalid settings file path: {targetPath}");
        var tempPath = Path.Combine(directory, $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        var backupPath = Path.Combine(directory, $"{Path.GetFileName(targetPath)}.bak");

        try
        {
            await using (var stream = File.Open(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            if (File.Exists(targetPath))
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(tempPath, targetPath, backupPath, ignoreMetadataErrors: true);

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            else
            {
                File.Move(tempPath, targetPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static string NormalizeValue(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
