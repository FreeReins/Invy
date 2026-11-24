using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies SpaceEntity to and from Space.</content>
internal static partial class EntityMapping
{
    public static Space ToContract(this SpaceEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Preset = entity.Preset,
        CreatedAt = entity.CreatedAt,
        Features = [.. entity.Features],
        Members = entity.Members.Select(member => new SpaceMember
        {
            UserId = member.UserId,
            Role = member.Role,
            GettingStartedDismissed = member.GettingStartedDismissed,
        }).ToList(),
    };

    public static SpaceEntity ToEntity(this Space space) => new()
    {
        Id = space.Id,
        Name = space.Name,
        Preset = space.Preset,
        CreatedAt = space.CreatedAt,
        Features = [.. space.Features],
        Members = space.Members.Select(member => new SpaceMemberEntity
        {
            SpaceId = space.Id,
            UserId = member.UserId,
            Role = member.Role,
            GettingStartedDismissed = member.GettingStartedDismissed,
        }).ToList(),
    };
}
