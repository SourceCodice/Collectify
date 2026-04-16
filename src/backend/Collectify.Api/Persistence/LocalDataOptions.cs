namespace Collectify.Api.Persistence;

public sealed class LocalDataOptions
{
    public string RootPath { get; set; } = "%LOCALAPPDATA%\\Collectify";
    public string DataFileName { get; set; } = "collectify-data.json";
    public string ImagesDirectoryName { get; set; } = "images";
}
