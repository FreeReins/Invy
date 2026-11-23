using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class SpaceMemberEntity
{
    public Guid SpaceId { get; set; }

    public Guid UserId { get; set; }

    public SpaceRole Role { get; set; }
}
