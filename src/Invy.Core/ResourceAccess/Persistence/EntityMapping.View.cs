using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies ViewEntity to and from View.</content>
internal static partial class EntityMapping
{
    public static View ToContract(this ViewEntity entity) => new()
    {
        Id = entity.Id,
        SpaceId = entity.SpaceId,
        OwnerId = entity.OwnerId,
        Name = entity.Name,
        Query = entity.Query,
        Display = entity.Display,
        Columns = [.. entity.Columns],
        Sort = entity.Sort,
        GroupBy = entity.GroupBy,
        Shared = entity.Shared,
        Pinned = entity.Pinned,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static ViewEntity ToEntity(this View view) => new()
    {
        Id = view.Id,
        SpaceId = view.SpaceId,
        OwnerId = view.OwnerId,
        Name = view.Name,
        Query = view.Query,
        Display = view.Display,
        Columns = [.. view.Columns],
        Sort = view.Sort,
        GroupBy = view.GroupBy,
        Shared = view.Shared,
        Pinned = view.Pinned,
        CreatedAt = view.CreatedAt,
        UpdatedAt = view.UpdatedAt,
    };
}
