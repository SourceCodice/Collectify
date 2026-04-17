using Collectify.Api.Persistence;

namespace Collectify.Api.Modules.DataTransfer;

public sealed class CollectifyExportDocument
{
    public string Format { get; set; } = DataTransferConstants.ExportFormat;
    public int FormatVersion { get; set; } = DataTransferConstants.CurrentFormatVersion;
    public DateTimeOffset ExportedAt { get; set; }
    public CollectifyDataDocument? Data { get; set; }
}

public sealed record BackupFileResponse(
    string Kind,
    string FileName,
    string SourcePath,
    string BackupPath,
    long SizeBytes);

public sealed record DataBackupResponse(
    DateTimeOffset CreatedAt,
    string BackupDirectoryPath,
    IReadOnlyList<BackupFileResponse> Files,
    IReadOnlyList<string> Messages);

public sealed record DataImportResponse(
    int ImportedCollections,
    int ImportedItems,
    int ImportedCategories,
    int ImportedTags,
    DateTimeOffset ImportedAt,
    IReadOnlyList<string> Messages);

internal static class DataTransferConstants
{
    public const string ExportFormat = "collectify-export";
    public const int CurrentFormatVersion = 1;
}
