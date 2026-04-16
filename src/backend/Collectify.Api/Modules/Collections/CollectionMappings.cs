namespace Collectify.Api.Modules.Collections;

public static class CollectionMappings
{
    public static CollectionSummaryResponse ToSummaryResponse(this Collection collection)
    {
        return new CollectionSummaryResponse(
            collection.Id,
            collection.Name,
            collection.Type,
            collection.Description,
            collection.Items.Count,
            collection.UpdatedAt);
    }

    public static CollectionDetailResponse ToDetailResponse(this Collection collection)
    {
        return new CollectionDetailResponse(
            collection.Id,
            collection.Name,
            collection.Type,
            collection.Description,
            collection.Items.Select(item => item.ToResponse()).ToList(),
            collection.CreatedAt,
            collection.UpdatedAt);
    }

    public static CollectionItemResponse ToResponse(this CollectionItem item)
    {
        return new CollectionItemResponse(
            item.Id,
            item.Title,
            item.Notes,
            item.Condition,
            item.AcquiredAt,
            item.UpdatedAt);
    }
}
