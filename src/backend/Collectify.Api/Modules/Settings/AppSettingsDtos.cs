namespace Collectify.Api.Modules.Settings;

public sealed record AppSettingsResponse(
    string DataRootPath,
    string DataFilePath,
    string ImagesPath,
    string BackupsPath,
    string SettingsFilePath,
    string Theme,
    bool AutomaticBackupEnabled,
    string Language,
    string Locale,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateAppSettingsRequest(
    string? DataRootPath,
    string? Theme,
    bool? AutomaticBackupEnabled,
    string? Language);
