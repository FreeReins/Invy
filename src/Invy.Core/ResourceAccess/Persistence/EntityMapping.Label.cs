using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies LabelEntity to and from Label.</content>
internal static partial class EntityMapping
{
    public static Label ToContract(this LabelEntity entity) => new()
    {
        Code = entity.Code,
        SpaceId = entity.SpaceId,
        ItemId = entity.ItemId,
        CreatedAt = entity.CreatedAt,
        PrintedAt = entity.PrintedAt,
    };

    public static LabelEntity ToEntity(this Label label) => new()
    {
        Code = label.Code,
        SpaceId = label.SpaceId,
        ItemId = label.ItemId,
        CreatedAt = label.CreatedAt,
        PrintedAt = label.PrintedAt,
    };
}
