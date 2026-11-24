using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;

namespace Invy.Api.Mcp;

/// <summary>
/// Who is calling a tool (the token's owner) and which of their spaces it is about. The <c>space</c> argument is a
/// space's name or id; without one, the person's only space is used, and a person in several is asked which.
/// </summary>
public sealed class McpCaller(IHttpContextAccessor http, ISpaceManager spaces)
{
    /// <summary>What every tool says about its <c>space</c> argument.</summary>
    public const string SpaceHelp =
        "The space's name or id, from list_spaces. Leave it out when the person has only one space; with several, Invy asks which.";

    /// <summary>The token owner's account id.</summary>
    /// <exception cref="NotSignedInException">The request has no signed-in account.</exception>
    public Guid UserId => Sessions.RequireUserId(http.HttpContext?.User);

    /// <summary>The space a tool call is about.</summary>
    /// <exception cref="NotFoundException">The person is in no space, or none matches.</exception>
    /// <exception cref="RuleViolationException">No space was named and the person is in several.</exception>
    public async Task<Guid> SpaceIdAsync(string? space, CancellationToken cancellationToken)
    {
        var mine = await spaces.ListSpacesAsync(UserId, cancellationToken);
        return Pick(mine, space).Id;
    }

    /// <summary>The space <paramref name="space"/> names among <paramref name="mine"/>: by id, then by name ignoring case.</summary>
    public static SpaceResponse Pick(IReadOnlyList<SpaceResponse> mine, string? space)
    {
        if (mine.Count == 0)
        {
            throw new NotFoundException("This account isn't in any space yet. Make one in Invy first.");
        }

        var named = space?.Trim() ?? string.Empty;
        if (named.Length == 0)
        {
            return mine.Count == 1
                ? mine[0]
                : throw new RuleViolationException($"This account is in more than one space: {Names(mine)}. Ask which one, and pass it as space.");
        }

        return mine.FirstOrDefault(candidate => Guid.TryParse(named, out var id) && candidate.Id == id)
            ?? mine.FirstOrDefault(candidate => string.Equals(candidate.Name, named, StringComparison.CurrentCultureIgnoreCase))
            ?? throw new NotFoundException($"There is no space called {named} for this account. Its spaces are: {Names(mine)}.");
    }

    private static string Names(IEnumerable<SpaceResponse> mine) => string.Join(", ", mine.Select(candidate => candidate.Name));
}
