using Collectify.Api.Persistence;

namespace Collectify.Api.Modules.Collections;

public sealed class ItemImageApplicationService(
    ICollectionRepository repository,
    LocalDataPathResolver pathResolver,
    ILogger<ItemImageApplicationService> logger)
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public async Task<ValidationResult<ItemImageResponse?>> AddImageAsync(
        Guid collectionId,
        Guid itemId,
        IFormFile file,
        string? caption,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        var errors = ValidateFile(file);
        if (errors.Count > 0)
        {
            return ValidationResult<ItemImageResponse?>.Failure(errors);
        }

        var collection = await repository.GetAsync(collectionId, cancellationToken);
        var item = collection?.Items.FirstOrDefault(current => current.Id == itemId);

        if (collection is null || item is null)
        {
            return ValidationResult<ItemImageResponse?>.Success(null);
        }

        var image = await CopyImageAsync(file, caption, isPrimary || item.Images.Count == 0, cancellationToken);
        ApplyPrimaryFlag(item, image);
        item.Images.Add(image);
        Touch(item, collection);

        await repository.SaveAsync(collection, cancellationToken);
        return ValidationResult<ItemImageResponse?>.Success(image.ToResponse());
    }

    public async Task<ValidationResult<ItemImageResponse?>> ReplaceImageAsync(
        Guid collectionId,
        Guid itemId,
        Guid imageId,
        IFormFile file,
        string? caption,
        bool? isPrimary,
        CancellationToken cancellationToken)
    {
        var errors = ValidateFile(file);
        if (errors.Count > 0)
        {
            return ValidationResult<ItemImageResponse?>.Failure(errors);
        }

        var collection = await repository.GetAsync(collectionId, cancellationToken);
        var item = collection?.Items.FirstOrDefault(current => current.Id == itemId);
        var currentImage = item?.Images.FirstOrDefault(image => image.Id == imageId);

        if (collection is null || item is null || currentImage is null)
        {
            return ValidationResult<ItemImageResponse?>.Success(null);
        }

        var replacement = await CopyImageAsync(
            file,
            caption ?? currentImage.Caption,
            isPrimary ?? currentImage.IsPrimary,
            cancellationToken);

        replacement.Id = currentImage.Id;
        replacement.CreatedAt = currentImage.CreatedAt;

        var index = item.Images.IndexOf(currentImage);
        item.Images[index] = replacement;
        ApplyPrimaryFlag(item, replacement);
        Touch(item, collection);

        await repository.SaveAsync(collection, cancellationToken);
        DeletePhysicalImage(currentImage.RelativePath);

        return ValidationResult<ItemImageResponse?>.Success(replacement.ToResponse());
    }

    public async Task<bool> DeleteImageAsync(Guid collectionId, Guid itemId, Guid imageId, CancellationToken cancellationToken)
    {
        var collection = await repository.GetAsync(collectionId, cancellationToken);
        var item = collection?.Items.FirstOrDefault(current => current.Id == itemId);
        var image = item?.Images.FirstOrDefault(current => current.Id == imageId);

        if (collection is null || item is null || image is null)
        {
            return false;
        }

        item.Images.Remove(image);

        if (image.IsPrimary && item.Images.Count > 0)
        {
            item.Images[0].IsPrimary = true;
            item.Images[0].UpdatedAt = DateTimeOffset.UtcNow;
        }

        Touch(item, collection);
        await repository.SaveAsync(collection, cancellationToken);
        DeletePhysicalImage(image.RelativePath);

        return true;
    }

    private async Task<ItemImage> CopyImageAsync(IFormFile file, string? caption, bool isPrimary, CancellationToken cancellationToken)
    {
        var paths = pathResolver.Resolve();
        Directory.CreateDirectory(paths.ImagesPath);

        var extension = GetExtension(file);
        var uniqueFileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
        var targetPath = Path.Combine(paths.ImagesPath, uniqueFileName);

        await using (var output = File.Open(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        await using (var input = file.OpenReadStream())
        {
            await input.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        var relativePath = Path.Combine("assets", "images", uniqueFileName).Replace('\\', '/');
        var now = DateTimeOffset.UtcNow;

        return new ItemImage
        {
            Id = Guid.NewGuid(),
            FileName = uniqueFileName,
            RelativePath = relativePath,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            Caption = Normalize(caption),
            IsPrimary = isPrimary,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private void DeletePhysicalImage(string relativePath)
    {
        try
        {
            var physicalPath = ResolveAssetPath(relativePath);

            if (physicalPath is not null && File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not delete local image asset {RelativePath}.", relativePath);
        }
    }

    private string? ResolveAssetPath(string relativePath)
    {
        var paths = pathResolver.Resolve();
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.GetFullPath(Path.Combine(paths.RootPath, normalized));
        var imagesRoot = Path.GetFullPath(paths.ImagesPath);

        return physicalPath.StartsWith(imagesRoot, StringComparison.OrdinalIgnoreCase)
            ? physicalPath
            : null;
    }

    private static void ApplyPrimaryFlag(Item item, ItemImage selectedImage)
    {
        if (!selectedImage.IsPrimary)
        {
            return;
        }

        foreach (var image in item.Images)
        {
            image.IsPrimary = image.Id == selectedImage.Id;
        }
    }

    private static void Touch(Item item, Collection collection)
    {
        var now = DateTimeOffset.UtcNow;
        item.UpdatedAt = now;
        collection.UpdatedAt = now;
    }

    private static Dictionary<string, string[]> ValidateFile(IFormFile file)
    {
        var errors = new Dictionary<string, string[]>();

        if (file.Length == 0)
        {
            errors["file"] = ["Image file is required."];
        }
        else if (!AllowedContentTypes.Contains(file.ContentType))
        {
            errors["file"] = ["Only JPEG, PNG, WebP and GIF images are supported."];
        }
        else if (file.Length > 10 * 1024 * 1024)
        {
            errors["file"] = ["Image cannot exceed 10 MB."];
        }

        return errors;
    }

    private static string GetExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);

        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant();
        }

        return file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".img"
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
