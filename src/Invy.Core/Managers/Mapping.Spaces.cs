using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Spaces and their members.</content>
internal static partial class Mapping
{
    public static SpaceResponse ToResponse(this Space space, Guid userId) =>
        new(space.Id, space.Name, space.Preset, space.Members.First(member => member.UserId == userId).Role, space.CreatedAt, [.. space.Features]);

    public static StarterPackResponse ToResponse(this StarterPack pack) =>
        new(
            pack.Key,
            pack.Name,
            pack.Description,
            pack.Types.Select(type => type.Name).ToList(),
            pack.Icon,
            [.. pack.Features],
            pack.Places is { } places ? new StarterPlacesResponse(places.Type, [.. places.Suggest]) : null,
            pack.Examples.Select(example => example.Name).ToList(),
            [.. pack.GettingStarted],
            new Dictionary<string, string>(pack.Hints));

    public static SpaceMemberResponse ToMemberResponse(this UserAccount user, SpaceRole role) =>
        new(user.Id, user.Username, user.DisplayName, role);
}
