using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Spaces and their members.</content>
internal static partial class Mapping
{
    public static SpaceResponse ToResponse(this Space space, Guid userId) =>
        new(space.Id, space.Name, space.Preset, space.Members.First(member => member.UserId == userId).Role, space.CreatedAt);

    public static StarterPackResponse ToResponse(this StarterPack pack) =>
        new(pack.Key, pack.Name, pack.Description, pack.Types.Select(type => type.Name).ToList());

    public static SpaceMemberResponse ToMemberResponse(this UserAccount user, SpaceRole role) =>
        new(user.Id, user.Username, user.DisplayName, role);
}
