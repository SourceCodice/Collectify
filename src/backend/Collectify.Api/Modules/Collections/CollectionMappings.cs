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
            collection.CategoryId,
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
            collection.CategoryId,
            collection.Items.Select(item => item.ToResponse()).ToList(),
            collection.CreatedAt,
            collection.UpdatedAt);
    }

    public static CollectionItemResponse ToResponse(this Item item)
    {
        return new CollectionItemResponse(
            item.Id,
            item.Title,
            item.Description,
            item.Notes,
            item.Condition,
            item.AcquiredAt,
            item.Attributes.Select(attribute => new ItemAttributeResponse(
                attribute.Id,
                attribute.Key,
                attribute.Label,
                attribute.Value,
                attribute.ValueType,
                attribute.Unit)).ToList(),
            item.TagIds,
            item.ExternalReferences.Select(reference => new ExternalReferenceResponse(
                reference.Id,
                reference.Provider,
                reference.ExternalId,
                reference.Url,
                reference.Metadata)).ToList(),
            item.UpdatedAt);
    }
}
