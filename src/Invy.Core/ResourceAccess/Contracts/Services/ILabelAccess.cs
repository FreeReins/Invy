using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>
/// The stored QR labels. Codes are unique across the install, so <see cref="FindAsync"/> and
/// <see cref="ExistingAsync"/> look in every space: a scanned code doesn't say which space it is from. Callers check
/// the person's role in the label's space before showing anything.
/// </summary>
internal interface ILabelAccess
{
    /// <summary>The label with this code in any space, or null when there is none.</summary>
    Task<Label?> FindAsync(string code, CancellationToken cancellationToken);

    /// <summary>Which of these codes are already taken, in any space.</summary>
    Task<HashSet<string>> ExistingAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken);

    /// <summary>How many labels the install has, in every space. Codes get longer as this grows.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>The label on an item, or null when it has none.</summary>
    Task<Label?> FindForItemAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>
    /// A space's labels by code, after <paramref name="after"/>, at most <paramref name="limit"/>. Only the unclaimed
    /// ones when <paramref name="unclaimed"/> is true.
    /// </summary>
    Task<List<Label>> ListAsync(Guid spaceId, bool unclaimed, string? after, int limit, CancellationToken cancellationToken);

    /// <summary>Adds the label, or changes the stored one.</summary>
    Task SaveAsync(Label label, CancellationToken cancellationToken);
}
