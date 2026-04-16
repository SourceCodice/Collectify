using System.Text.Json;

namespace Collectify.Api.Persistence;

public sealed class JsonCollectifyDataStore(
    LocalDataPathResolver pathResolver,
    ILogger<JsonCollectifyDataStore> logger) : ICollectifyDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<CollectifyDataDocument> LoadAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var paths = pathResolver.Resolve();
            Directory.CreateDirectory(paths.RootPath);
            Directory.CreateDirectory(paths.ImagesPath);
            Directory.CreateDirectory(paths.BackupsPath);

            if (!File.Exists(paths.DataFilePath))
            {
                var initialDocument = CollectifySeedData.Create();
                await WriteAtomicAsync(paths.DataFilePath, initialDocument, cancellationToken);
                return initialDocument;
            }

            try
            {
                await using var stream = File.Open(paths.DataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var document = await JsonSerializer.DeserializeAsync<CollectifyDataDocument>(
                    stream,
                    SerializerOptions,
                    cancellationToken);

                if (document is null)
                {
                    throw new JsonException("Collectify data file is empty.");
                }

                Normalize(document);
                return document;
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                var corruptPath = BuildCorruptFilePath(paths.DataFilePath);
                File.Move(paths.DataFilePath, corruptPath);

                logger.LogWarning(
                    exception,
                    "Collectify data file was corrupted and moved to {CorruptPath}. A new file will be created.",
                    corruptPath);

                var replacementDocument = CollectifySeedData.Create();
                await WriteAtomicAsync(paths.DataFilePath, replacementDocument, cancellationToken);
                return replacementDocument;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(CollectifyDataDocument document, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var paths = pathResolver.Resolve();
            Directory.CreateDirectory(paths.RootPath);
            Directory.CreateDirectory(paths.ImagesPath);
            Directory.CreateDirectory(paths.BackupsPath);

            document.UpdatedAt = DateTimeOffset.UtcNow;
            Normalize(document);

            if (paths.AutomaticBackupEnabled)
            {
                CreateAutomaticBackup(paths.DataFilePath, paths.BackupsPath);
            }

            await WriteAtomicAsync(paths.DataFilePath, document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static void CreateAutomaticBackup(string dataFilePath, string backupsPath)
    {
        if (!File.Exists(dataFilePath))
        {
            return;
        }

        Directory.CreateDirectory(backupsPath);
        var backupFileName = $"{Path.GetFileNameWithoutExtension(dataFilePath)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.json";
        File.Copy(dataFilePath, Path.Combine(backupsPath, backupFileName), overwrite: false);
    }

    private async Task WriteAtomicAsync(string targetPath, CollectifyDataDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException($"Invalid data file path: {targetPath}");

        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        var backupPath = Path.Combine(directory, $"{Path.GetFileName(targetPath)}.bak");

        try
        {
            await using (var stream = File.Open(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
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

    private static void Normalize(CollectifyDataDocument document)
    {
        document.UserProfile ??= new();
        document.AppSettings ??= new();
        document.Categories ??= [];
        document.Tags ??= [];
        document.Collections ??= [];

        foreach (var collection in document.Collections)
        {
            collection.Items ??= [];

            foreach (var item in collection.Items)
            {
                item.CollectionId = collection.Id;
                item.Attributes ??= [];
                item.TagIds ??= [];
                item.Images ??= [];
                item.ExternalReferences ??= [];
            }
        }
    }

    private static string BuildCorruptFilePath(string dataFilePath)
    {
        var directory = Path.GetDirectoryName(dataFilePath)
            ?? throw new InvalidOperationException($"Invalid data file path: {dataFilePath}");
        var fileName = Path.GetFileName(dataFilePath);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");

        return Path.Combine(directory, $"{fileName}.corrupt-{timestamp}");
    }
}
