using System.Text.Json;
using System.Text.Json.Serialization;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies items, their events and their sortable values to and from their entities.</content>
internal static partial class EntityMapping
{
    public static Item ToContract(this ItemEntity entity) => new()
    {
        Id = entity.Id,
        SpaceId = entity.SpaceId,
        TypeId = entity.TypeId,
        Name = entity.Name,
        Marking = entity.Marking,
        LabelCode = entity.LabelCode,
        ParentId = entity.ParentId,
        Path = entity.Path,
        Depth = entity.Depth,
        HoldsItems = entity.HoldsItems,
        Quantity = entity.Quantity,
        Unit = entity.Unit,
        Fields = JsonSerializer.Deserialize(entity.Fields, ItemJson.Default.DictionaryGuidJsonElement) ?? [],
        Tags = [.. entity.Tags],
        Notes = entity.Notes,
        Status = entity.Status,
        LastSeenAt = entity.LastSeenAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Value = entity.Value,
        ContentCount = entity.ContentCount,
        ContentValue = entity.ContentValue,
        Version = entity.Version,
    };

    public static ItemEntity ToEntity(this Item item) => new()
    {
        Id = item.Id,
        SpaceId = item.SpaceId,
        TypeId = item.TypeId,
        Name = item.Name,
        Marking = item.Marking,
        LabelCode = item.LabelCode,
        ParentId = item.ParentId,
        Path = item.Path,
        Depth = item.Depth,
        HoldsItems = item.HoldsItems,
        Quantity = item.Quantity,
        Unit = item.Unit,
        Fields = JsonSerializer.Serialize(item.Fields, ItemJson.Default.DictionaryGuidJsonElement),
        Tags = [.. item.Tags],
        Notes = item.Notes,
        Status = item.Status,
        LastSeenAt = item.LastSeenAt,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        Value = item.Value,
        ContentCount = item.ContentCount,
        ContentValue = item.ContentValue,
        Version = item.Version,
    };

    public static ItemEvent ToContract(this ItemEventEntity entity) => new()
    {
        Id = entity.Id,
        SpaceId = entity.SpaceId,
        ItemId = entity.ItemId,
        At = entity.At,
        ActorId = entity.ActorId,
        Kind = entity.Kind,
        FromParent = entity.FromParent,
        ToParent = entity.ToParent,
        QtyDelta = entity.QtyDelta,
        Changes = entity.Data is null ? [] : JsonSerializer.Deserialize(entity.Data, ItemJson.Default.ListItemChange) ?? [],
    };

    public static ItemEventEntity ToEntity(this ItemEvent itemEvent) => new()
    {
        Id = itemEvent.Id,
        SpaceId = itemEvent.SpaceId,
        ItemId = itemEvent.ItemId,
        At = itemEvent.At,
        ActorId = itemEvent.ActorId,
        Kind = itemEvent.Kind,
        FromParent = itemEvent.FromParent,
        ToParent = itemEvent.ToParent,
        QtyDelta = itemEvent.QtyDelta,
        Data = itemEvent.Changes.Count == 0 ? null : JsonSerializer.Serialize(itemEvent.Changes, ItemJson.Default.ListItemChange),
    };

    public static ItemValueEntity ToEntity(this ItemValue value) => new()
    {
        SpaceId = value.SpaceId,
        ItemId = value.ItemId,
        FieldId = value.FieldId,
        Num = value.Num,
        Date = value.Date,
        Text = value.Text,
    };

    /// <summary>The JSON stored in items' and events' jsonb columns, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(Dictionary<Guid, JsonElement>))]
    [JsonSerializable(typeof(List<ItemChange>))]
    private sealed partial class ItemJson : JsonSerializerContext;
}
