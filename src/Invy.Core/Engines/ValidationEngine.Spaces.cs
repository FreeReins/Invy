using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Engines;

/// <content>Checks on spaces, their members and roles.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>The longest name a space can have.</summary>
    public const int SpaceNameLength = 80;

    private const string NoSuchSpace = "That space doesn't exist, or you aren't a member of it. Pick another space, or ask its owner to add you.";

    /// <summary>
    /// The space, once the person is known to have at least <paramref name="atLeast"/> in it. Someone who isn't a
    /// member is told the space isn't there, so ids can't be used to find out which spaces exist. Install admins get
    /// no access from being admin.
    /// </summary>
    /// <exception cref="NotFoundException">There is no such space, or the person isn't in it.</exception>
    /// <exception cref="NotAllowedException">Their role is too low.</exception>
    public async Task<Space> SpaceRoleAsync(Guid spaceId, Guid userId, SpaceRole atLeast, CancellationToken cancellationToken)
    {
        var space = await spaces.FindAsync(spaceId, cancellationToken) ?? throw new NotFoundException(NoSuchSpace);
        var member = space.Members.FirstOrDefault(m => m.UserId == userId) ?? throw new NotFoundException(NoSuchSpace);
        if (member.Role < atLeast)
        {
            throw new NotAllowedException(atLeast == SpaceRole.Owner
                ? $"Only an owner of {space.Name} can do that. Ask one of its owners."
                : $"You can only look in {space.Name}. Ask one of its owners to make you an editor.");
        }

        return space;
    }

    /// <summary>A space's name, trimmed.</summary>
    public static string SpaceName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        return trimmed.Length is > 0 and <= SpaceNameLength
            ? trimmed
            : throw new RuleViolationException($"A space needs a name of 1 to {SpaceNameLength} characters.");
    }

    /// <summary>The starter pack a new space asks for, or null when it asks for none.</summary>
    public StarterPack? StarterPack(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var packs = starterPacks.All();
        return packs.FirstOrDefault(pack => string.Equals(pack.Key, key.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new RuleViolationException(
                $"There is no starter pack called {key.Trim()}. Pick one of {string.Join(", ", packs.Select(pack => pack.Key))}, or none.");
    }

    public static SpaceRole Role(SpaceRole role) =>
        Enum.IsDefined(role) ? role : throw new RuleViolationException("Pick a role: Owner, Editor or Viewer.");

    /// <summary>The account to add to a space, once it is known to exist and not to be in it already.</summary>
    public async Task<UserAccount> NewMemberAsync(Space space, string? username, CancellationToken cancellationToken)
    {
        var normalized = Usernames.Normalize(username);
        var user = await accounts.FindUserByUsernameAsync(normalized, cancellationToken)
            ?? throw new NotFoundException($"There is no account called {normalized}. An admin can invite them first.");
        return space.Members.Any(member => member.UserId == user.Id)
            ? throw new ConflictException($"{user.DisplayName} is already in {space.Name}.")
            : user;
    }

    public static SpaceMember Member(Space space, Guid userId) =>
        space.Members.FirstOrDefault(member => member.UserId == userId)
        ?? throw new NotFoundException($"That person isn't in {space.Name}.");

    /// <summary>Checks that a space, as it would be after a change, still has an owner.</summary>
    public static void KeepsAnOwner(Space space)
    {
        if (!space.Members.Any(member => member.Role == SpaceRole.Owner))
        {
            throw new RuleViolationException($"{space.Name} needs at least one owner. Make someone else an owner first.");
        }
    }
}
