namespace Collectify.Api.Persistence;

public sealed record LocalDataPaths(
    string RootPath,
    string DataFilePath,
    string ImagesPath,
    string BackupsPath,
    string SettingsFilePath,
    bool AutomaticBackupEnabled);
