namespace Collectify.Api.Persistence;

public sealed class LocalDataOptions
{
    public string RootPath { get; set; } = "%LOCALAPPDATA%\\Collectify";
    public string DataFileName { get; set; } = "collectify-data.json";
    public string ImagesDirectoryName { get; set; } = "images";
    public string BackupsDirectoryName { get; set; } = "backups";
    public string SettingsFileName { get; set; } = "settings.json";
}
