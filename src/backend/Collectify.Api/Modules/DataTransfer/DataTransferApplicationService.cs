using System.Text.Json;
using Collectify.Api.Modules.Collections;
using Collectify.Api.Modules.Settings;
using Collectify.Api.Persistence;

namespace Collectify.Api.Modules.DataTransfer;

public sealed class DataTransferApplicationService(
    ICollectifyDataStore dataStore,
    LocalDataPathResolver pathResolver,
    AppSettingsFileStore settingsStore,
    ILogger<DataTransferApplicationService> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<DataBackupResponse> CreateBackupAsync(CancellationToken cancellationToken)
    {
        await dataStore.LoadAsync(cancellationToken);
        await settingsStore.GetAsync(cancellationToken);

        var paths = pathResolver.Resolve();
        Directory.CreateDirectory(paths.BackupsPath);

        var createdAt = DateTimeOffset.UtcNow;
        var backupDirectory = Path.Combine(paths.BackupsPath, $"manual-{createdAt:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(backupDirectory);

        var files = new List<BackupFileResponse>();
        var messages = new List<string>();

        AddBackupFile("Dati collezioni", paths.DataFilePath, backupDirectory, files, messages);
        AddBackupFile("Impostazioni", paths.SettingsFilePath, backupDirectory, files, messages);

        if (files.Count == 0)
        {
            messages.Add("Nessun file JSON principale trovato per il backup.");
        }

        logger.LogInformation("Created local data backup in {BackupDirectoryPath} with {FileCount} file(s).", backupDirectory, files.Count);

        return new DataBackupResponse(createdAt, backupDirectory, files, messages);
    }

    public async Task<CollectifyExportDocument> BuildExportAsync(CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var exportedAt = DateTimeOffset.UtcNow;

        logger.LogInformation("Created Collectify export with {CollectionCount} collection(s).", document.Collections.Count);

        return new CollectifyExportDocument
        {
            Format = DataTransferConstants.ExportFormat,
            FormatVersion = DataTransferConstants.CurrentFormatVersion,
            ExportedAt = exportedAt,
            Data = document
        };
    }

    public async Task<ValidationResult<DataImportResponse>> ImportAsync(Stream input, CancellationToken cancellationToken)
    {
        CollectifyExportDocument? exportDocument;

        try
        {
            exportDocument = await JsonSerializer.DeserializeAsync<CollectifyExportDocument>(
                input,
                SerializerOptions,
                cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return ValidationResult<DataImportResponse>.Failure(new Dictionary<string, string[]>
            {
                ["file"] = ["Il file selezionato non e' un JSON di export Collectify valido."]
            });
        }

        var validationErrors = ValidateExportDocument(exportDocument);
        if (validationErrors.Count > 0)
        {
            return ValidationResult<DataImportResponse>.Failure(validationErrors);
        }

        var importedData = exportDocument!.Data!;
        var response = await dataStore.UpdateAsync(
            currentDocument => DataStoreUpdate<DataImportResponse>.Changed(ImportIntoDocument(importedData, currentDocument)),
            cancellationToken);

        logger.LogInformation(
            "Imported Collectify data: {CollectionCount} collection(s), {ItemCount} item(s).",
            response.ImportedCollections,
            response.ImportedItems);

        return ValidationResult<DataImportResponse>.Success(response);
    }

    public static byte[] SerializeExport(CollectifyExportDocument exportDocument)
    {
        return JsonSerializer.SerializeToUtf8Bytes(exportDocument, SerializerOptions);
    }

    private static DataImportResponse ImportIntoDocument(CollectifyDataDocument importedData, CollectifyDataDocument currentDocument)
    {
        var now = DateTimeOffset.UtcNow;
        var messages = new List<string>();
        var categoryIdMap = new Dictionary<Guid, Guid>();
        var tagIdMap = new Dictionary<Guid, Guid>();
        var importedCategories = 0;
        var importedTags = 0;
        var importedCollections = 0;
        var importedItems = 0;

        foreach (var category in importedData.Categories)
        {
            var existingCategory = currentDocument.Categories.FirstOrDefault(current =>
                string.Equals(current.Name, category.Name, StringComparison.OrdinalIgnoreCase));

            if (existingCategory is not null)
            {
                categoryIdMap[category.Id] = existingCategory.Id;
                continue;
            }

            var categoryId = Guid.NewGuid();
            categoryIdMap[category.Id] = categoryId;
            currentDocument.Categories.Add(new CollectionCategory
            {
                Id = categoryId,
                Name = category.Name.Trim(),
                Description = category.Description,
                Icon = category.Icon,
                Color = category.Color,
                SortOrder = category.SortOrder,
                CreatedAt = now,
                UpdatedAt = now
            });
            importedCategories++;
        }

        foreach (var tag in importedData.Tags)
        {
            var existingTag = currentDocument.Tags.FirstOrDefault(current =>
                string.Equals(current.Name, tag.Name, StringComparison.OrdinalIgnoreCase));

            if (existingTag is not null)
            {
                tagIdMap[tag.Id] = existingTag.Id;
                continue;
            }

            var tagId = Guid.NewGuid();
            tagIdMap[tag.Id] = tagId;
            currentDocument.Tags.Add(new Tag
            {
                Id = tagId,
                Name = tag.Name.Trim(),
                Color = tag.Color,
                CreatedAt = now,
                UpdatedAt = now
            });
            importedTags++;
        }

        var existingCollectionNames = currentDocument.Collections
            .Select(collection => collection.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var collection in importedData.Collections)
        {
            var collectionId = Guid.NewGuid();
            var importedCollection = new Collection
            {
                Id = collectionId,
                CategoryId = collection.CategoryId.HasValue && categoryIdMap.TryGetValue(collection.CategoryId.Value, out var mappedCategoryId)
                    ? mappedCategoryId
                    : null,
                Name = BuildUniqueName(collection.Name.Trim(), existingCollectionNames),
                Type = string.IsNullOrWhiteSpace(collection.Type) ? "Custom" : collection.Type.Trim(),
                Description = collection.Description,
                CreatedAt = now,
                UpdatedAt = now,
                Items = []
            };

            existingCollectionNames.Add(importedCollection.Name);

            foreach (var item in collection.Items)
            {
                importedCollection.Items.Add(CloneImportedItem(item, collectionId, tagIdMap, now));
                importedItems++;
            }

            currentDocument.Collections.Add(importedCollection);
            importedCollections++;
        }

        messages.Add("Import completato senza sovrascrivere le collezioni esistenti.");
        if (importedData.Collections.Any(collection => collection.Items.Any(item => item.Images.Count > 0)))
        {
            messages.Add("Il file JSON mantiene i riferimenti alle immagini locali; assicurati che i file immagine esistano nella cartella dati corrente.");
        }

        return new DataImportResponse(
            importedCollections,
            importedItems,
            importedCategories,
            importedTags,
            now,
            messages);
    }

    private static Item CloneImportedItem(
        Item item,
        Guid collectionId,
        IReadOnlyDictionary<Guid, Guid> tagIdMap,
        DateTimeOffset now)
    {
        return new Item
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            Title = item.Title.Trim(),
            Description = item.Description,
            Notes = item.Notes,
            Condition = string.IsNullOrWhiteSpace(item.Condition) ? "Non specificato" : item.Condition.Trim(),
            AcquiredAt = item.AcquiredAt,
            EstimatedValue = item.EstimatedValue,
            Currency = item.Currency,
            TagIds = item.TagIds
                .Select(tagId => tagIdMap.TryGetValue(tagId, out var mappedTagId) ? mappedTagId : (Guid?)null)
                .Where(mappedTagId => mappedTagId.HasValue)
                .Select(mappedTagId => mappedTagId!.Value)
                .Distinct()
                .ToList(),
            Attributes = item.Attributes.Select(attribute => new ItemAttribute
            {
                Id = Guid.NewGuid(),
                Key = attribute.Key.Trim(),
                Label = string.IsNullOrWhiteSpace(attribute.Label) ? attribute.Key.Trim() : attribute.Label.Trim(),
                Value = attribute.Value,
                ValueType = string.IsNullOrWhiteSpace(attribute.ValueType) ? "Text" : attribute.ValueType.Trim(),
                Unit = attribute.Unit,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList(),
            Images = item.Images.Select(image => new ItemImage
            {
                Id = Guid.NewGuid(),
                FileName = image.FileName,
                RelativePath = image.RelativePath,
                ContentType = image.ContentType,
                SizeBytes = image.SizeBytes,
                Caption = image.Caption,
                IsPrimary = image.IsPrimary,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList(),
            ExternalReferences = item.ExternalReferences.Select(reference => new ExternalReference
            {
                Id = Guid.NewGuid(),
                Provider = reference.Provider.Trim(),
                ExternalId = reference.ExternalId,
                Url = reference.Url,
                Metadata = reference.Metadata is null
                    ? []
                    : new Dictionary<string, string>(reference.Metadata, StringComparer.OrdinalIgnoreCase),
                CreatedAt = now,
                UpdatedAt = now
            }).ToList(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void AddBackupFile(
        string kind,
        string sourcePath,
        string backupDirectory,
        ICollection<BackupFileResponse> files,
        ICollection<string> messages)
    {
        if (!File.Exists(sourcePath))
        {
            messages.Add($"{kind}: file non trovato, backup saltato.");
            return;
        }

        var fileName = Path.GetFileName(sourcePath);
        var backupPath = Path.Combine(backupDirectory, fileName);
        File.Copy(sourcePath, backupPath, overwrite: false);

        files.Add(new BackupFileResponse(
            kind,
            fileName,
            sourcePath,
            backupPath,
            new FileInfo(backupPath).Length));
    }

    private static Dictionary<string, string[]> ValidateExportDocument(CollectifyExportDocument? exportDocument)
    {
        var errors = new Dictionary<string, string[]>();

        if (exportDocument is null)
        {
            errors["file"] = ["Il file di import e' vuoto o non leggibile."];
            return errors;
        }

        if (!string.Equals(exportDocument.Format, DataTransferConstants.ExportFormat, StringComparison.Ordinal))
        {
            errors["format"] = ["Il file non e' un export Collectify riconosciuto."];
        }

        if (exportDocument.FormatVersion != DataTransferConstants.CurrentFormatVersion)
        {
            errors["formatVersion"] = [$"Versione export non supportata: {exportDocument.FormatVersion}."];
        }

        if (exportDocument.Data is null)
        {
            errors["data"] = ["Il file non contiene dati Collectify."];
            return errors;
        }

        exportDocument.Data.Categories ??= [];
        exportDocument.Data.Tags ??= [];
        exportDocument.Data.Collections ??= [];

        var collectionErrors = new List<string>();
        foreach (var collection in exportDocument.Data.Collections)
        {
            collection.Items ??= [];

            if (string.IsNullOrWhiteSpace(collection.Name))
            {
                collectionErrors.Add("Una collezione importata non ha un nome.");
            }

            foreach (var item in collection.Items)
            {
                item.Attributes ??= [];
                item.TagIds ??= [];
                item.Images ??= [];
                item.ExternalReferences ??= [];

                if (string.IsNullOrWhiteSpace(item.Title))
                {
                    collectionErrors.Add($"La collezione '{collection.Name}' contiene un elemento senza titolo.");
                }
            }
        }

        var invalidCategories = exportDocument.Data.Categories
            .Where(category => string.IsNullOrWhiteSpace(category.Name))
            .Select(_ => "Una categoria importata non ha un nome.");
        var invalidTags = exportDocument.Data.Tags
            .Where(tag => string.IsNullOrWhiteSpace(tag.Name))
            .Select(_ => "Un tag importato non ha un nome.");

        var dataErrors = collectionErrors.Concat(invalidCategories).Concat(invalidTags).Distinct().ToArray();
        if (dataErrors.Length > 0)
        {
            errors["data"] = dataErrors;
        }

        return errors;
    }

    private static string BuildUniqueName(string name, ISet<string> existingNames)
    {
        var baseName = string.IsNullOrWhiteSpace(name) ? "Collezione importata" : name;
        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }

        var index = 2;
        while (existingNames.Contains($"{baseName} (importata {index})"))
        {
            index++;
        }

        return $"{baseName} (importata {index})";
    }
}
