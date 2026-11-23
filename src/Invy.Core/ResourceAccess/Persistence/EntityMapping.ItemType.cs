using System.Text.Json;
using System.Text.Json.Serialization;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies ItemTypeEntity to and from ItemType.</content>
internal static partial class EntityMapping
{
    public static ItemType ToContract(this ItemTypeEntity entity) => new()
    {
        Id = entity.Id,
        SpaceId = entity.SpaceId,
        Name = entity.Name,
        Icon = entity.Icon,
        Colour = entity.Colour,
        HoldsItems = entity.HoldsItems,
        Fields = JsonSerializer.Deserialize(entity.Fields, FieldJson.Default.ListTypeField) ?? [],
        Version = entity.Version,
    };

    public static ItemTypeEntity ToEntity(this ItemType type) => new()
    {
        Id = type.Id,
        SpaceId = type.SpaceId,
        Name = type.Name,
        Icon = type.Icon,
        Colour = type.Colour,
        HoldsItems = type.HoldsItems,
        Fields = JsonSerializer.Serialize(type.Fields, FieldJson.Default.ListTypeField),
        Version = type.Version,
    };

    /// <summary>The JSON shape of a type's fields, as stored in its row, made at build time. Kinds are written by name.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(List<TypeField>))]
    private sealed partial class FieldJson : JsonSerializerContext;
}
