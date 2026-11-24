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

    /// <summary>The features a space can switch on and off, in the order their switches are shown.</summary>
    IReadOnlyList<SpaceFeatureResponse> ListFeatures();

    /// <summary>
    /// Makes a space with the person as its owner, filled with the starter pack's types if one is chosen, with its
    /// features (the kind's when none are given), the places asked for and, if asked, the kind's examples, in one commit.
    /// </summary>
    /// <exception cref="RuleViolationException">
    /// The name is blank or too long, there is no such starter pack or feature, or there are too many places.
    /// </exception>
    Task<SpaceResponse> CreateSpaceAsync(Guid userId, CreateSpaceRequest request, CancellationToken cancellationToken);

    /// <summary>Switches a space's features on and off: the ones given are on, the rest off. Owners only.</summary>
    /// <exception cref="RuleViolationException">There is no feature with one of the keys.</exception>
    Task<SpaceResponse> SetFeaturesAsync(Guid userId, Guid spaceId, SpaceFeaturesRequest request, CancellationToken cancellationToken);

    /// <summary>Changes a space's kind, which changes only its hints and getting-started steps, never its types. Owners only.</summary>
    /// <exception cref="RuleViolationException">There is no such starter pack.</exception>
    Task<SpaceResponse> SetKindAsync(Guid userId, Guid spaceId, SpaceKindRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the space's example items and their timelines in one go. One still holding something of your own is
    /// kept. Editors and owners.
    /// </summary>
    Task<RemoveExamplesResponse> RemoveExamplesAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>The getting-started card for the person: the kind's steps, each worked out from what the space has. Any member.</summary>
    Task<GettingStartedResponse> GetGettingStartedAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>Puts the person's getting-started card away (or, with false, shows it again). It changes nobody else's. Any member.</summary>
    Task<GettingStartedResponse> DismissGettingStartedAsync(Guid userId, Guid spaceId, bool dismissed, CancellationToken cancellationToken);

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

    /// <summary>The address of the space's calendar feed, or none while it is off. Any member can see it.</summary>
    Task<CalendarFeedResponse> GetCalendarFeedAsync(Guid userId, Guid spaceId, Uri site, CancellationToken cancellationToken);

    /// <summary>Turns the calendar feed on with a new address; one made before stops working. Owners only.</summary>
    Task<CalendarFeedResponse> StartCalendarFeedAsync(Guid userId, Guid spaceId, Uri site, CancellationToken cancellationToken);

    /// <summary>Turns the calendar feed off. Owners only.</summary>
    Task<CalendarFeedResponse> StopCalendarFeedAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>
    /// The feed itself, as iCalendar text: reminder dates and loans' due dates. Needs no sign-in, only the secret.
    /// </summary>
    /// <exception cref="NotFoundException">The feed is off or the secret is wrong.</exception>
    Task<string> GetCalendarAsync(Guid spaceId, string token, Uri site, CancellationToken cancellationToken);
}
