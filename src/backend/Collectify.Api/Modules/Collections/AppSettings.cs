namespace Collectify.Api.Modules.Collections;

public sealed class AppSettings
{
    public Guid Id { get; set; }
    public string DataRootPath { get; set; } = string.Empty;
    public string Theme { get; set; } = "System";
    public bool AutomaticBackupEnabled { get; set; } = true;
    public string Language { get; set; } = "it-IT";
    public string Locale { get; set; } = "it-IT";
    public string Currency { get; set; } = "EUR";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public int DataSchemaVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
