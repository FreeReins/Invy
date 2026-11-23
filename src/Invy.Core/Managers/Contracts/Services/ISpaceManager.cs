using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// Spaces and the people in them. Every call takes the asking person's account id and checks their role in the
/// space first: viewers can look, owners manage the space and its members. Being an install admin grants nothing here.
/// </summary>
public interface ISpaceManager
{
    /// <summary>The spaces the person is a member of, oldest first.</summary>
    Task<IReadOnlyList<SpaceResponse>> ListSpacesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The starter packs of types a new space can begin with.</summary>
    IReadOnlyList<StarterPackResponse> ListStarterPacks();

    /// <summary>Makes a space with the person as its owner, filled with the starter pack's types if one is chosen.</summary>
    /// <exception cref="RuleViolationException">The name is blank or too long, or there is no such starter pack.</exception>
    Task<SpaceResponse> CreateSpaceAsync(Guid userId, CreateSpaceRequest request, CancellationToken cancellationToken);

    /// <summary>Renames a space. Owners only.</summary>
    Task<SpaceResponse> RenameSpaceAsync(Guid userId, Guid spaceId, RenameSpaceRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes a space and everything in it. Owners only.</summary>
    Task DeleteSpaceAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>The people in a space, owners first, then by name. Any member can look.</summary>
    Task<IReadOnlyList<SpaceMemberResponse>> ListMembersAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>Adds someone who has an account on this install. Owners only. Returns the members.</summary>
    /// <exception cref="NotFoundException">There is no account with that username.</exception>
    /// <exception cref="ConflictException">They are already a member.</exception>
    Task<IReadOnlyList<SpaceMemberResponse>> AddMemberAsync(
        Guid userId, Guid spaceId, AddSpaceMemberRequest request, CancellationToken cancellationToken);

    /// <summary>Changes a member's role. Owners only. A space always keeps at least one owner. Returns the members.</summary>
    Task<IReadOnlyList<SpaceMemberResponse>> SetMemberRoleAsync(
        Guid userId, Guid spaceId, Guid memberId, SpaceMemberRoleRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Takes someone out of a space. Owners can remove anyone and anyone can leave, but the last owner can't.
    /// Returns the members.
    /// </summary>
    Task<IReadOnlyList<SpaceMemberResponse>> RemoveMemberAsync(Guid userId, Guid spaceId, Guid memberId, CancellationToken cancellationToken);
}
