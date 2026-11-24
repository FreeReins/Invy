using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.Utilities;

namespace Invy.Core.Managers;

/// <inheritdoc cref="ISpaceManager"/>
internal sealed partial class SpaceManager(
    ISpaceAccess spaces,
    ITypeAccess types,
    IItemAccess items,
    ILabelAccess labels,
    IAttachmentAccess attachments,
    IProjectAccess projects,
    IAccountAccess accounts,
    IStarterPackAccess starterPacks,
    IFileStoreAccess files,
    IMessageAccess messages,
    ValidationEngine validation,
    ItemEngine itemEngine,
    CalendarEngine calendar,
    TimeProvider clock) : ISpaceManager
{
    public async Task<IReadOnlyList<SpaceResponse>> ListSpacesAsync(Guid userId, CancellationToken cancellationToken) =>
        (await spaces.ListForUserAsync(userId, cancellationToken)).Select(space => space.ToResponse(userId)).ToList();

    public IReadOnlyList<StarterPackResponse> ListStarterPacks() =>
        starterPacks.All().Select(pack => pack.ToResponse()).ToList();

    public IReadOnlyList<SpaceFeatureResponse> ListFeatures() =>
        SpaceFeatures.All.Select(feature => new SpaceFeatureResponse(feature.Key, feature.Name, feature.Description, feature.Available)).ToList();

    public async Task<SpaceResponse> RenameSpaceAsync(Guid userId, Guid spaceId, RenameSpaceRequest request, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        space.Name = ValidationEngine.SpaceName(request.Name);
        await spaces.SaveAsync(space, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return space.ToResponse(userId);
    }

    public async Task DeleteSpaceAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        await spaces.RemoveAsync(spaceId, cancellationToken);
        await messages.CommitAsync(cancellationToken);

        // Its attached files go once its rows have. Backups keep their own copies.
        files.DeleteSpace(spaceId);
    }

    public async Task<IReadOnlyList<SpaceMemberResponse>> ListMembersAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken) =>
        await MembersAsync(await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<SpaceMemberResponse>> AddMemberAsync(
        Guid userId, Guid spaceId, AddSpaceMemberRequest request, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        var role = ValidationEngine.Role(request.Role);
        var user = await validation.NewMemberAsync(space, request.Username, cancellationToken);
        space.Members.Add(new SpaceMember { UserId = user.Id, Role = role });
        return await SaveAsync(space, cancellationToken);
    }

    public async Task<IReadOnlyList<SpaceMemberResponse>> SetMemberRoleAsync(
        Guid userId, Guid spaceId, Guid memberId, SpaceMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        ValidationEngine.Member(space, memberId).Role = ValidationEngine.Role(request.Role);
        ValidationEngine.KeepsAnOwner(space);
        return await SaveAsync(space, cancellationToken);
    }

    public async Task<IReadOnlyList<SpaceMemberResponse>> RemoveMemberAsync(
        Guid userId, Guid spaceId, Guid memberId, CancellationToken cancellationToken)
    {
        // Anyone can leave; removing someone else takes an owner.
        var space = await validation.SpaceRoleAsync(
            spaceId, userId, memberId == userId ? SpaceRole.Viewer : SpaceRole.Owner, cancellationToken);
        space.Members.Remove(ValidationEngine.Member(space, memberId));
        ValidationEngine.KeepsAnOwner(space);
        return await SaveAsync(space, cancellationToken);
    }

    private async Task<IReadOnlyList<SpaceMemberResponse>> SaveAsync(Space space, CancellationToken cancellationToken)
    {
        await spaces.SaveAsync(space, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await MembersAsync(space, cancellationToken);
    }

    /// <summary>Owners first, then editors, then viewers, each by name. Accounts removed since are left out.</summary>
    private async Task<IReadOnlyList<SpaceMemberResponse>> MembersAsync(Space space, CancellationToken cancellationToken)
    {
        var users = (await accounts.ListUsersAsync(cancellationToken)).ToDictionary(user => user.Id);
        return space.Members
            .Where(member => users.ContainsKey(member.UserId))
            .Select(member => users[member.UserId].ToMemberResponse(member.Role))
            .OrderByDescending(member => member.Role)
            .ThenBy(member => member.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
