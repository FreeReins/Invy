using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// QR labels: batches of codes printed before anything exists, claimed by scanning. A code is unique across the install,
/// so scanning one finds its space. Viewers look; editors and owners make, claim and attach labels.
/// </summary>
public interface ILabelManager
{
    /// <summary>Makes a batch of new unclaimed codes in the space, by code. Codes are 4 to 6 characters without look-alikes.</summary>
    /// <exception cref="RuleViolationException">The count is outside 1 to 500.</exception>
    Task<IReadOnlyList<LabelResponse>> CreateLabelsAsync(Guid userId, Guid spaceId, CreateLabelsRequest request, CancellationToken cancellationToken);

    /// <summary>The space's labels by code, 100 at a time; only the unclaimed ones when <paramref name="unclaimed"/> is true.</summary>
    Task<LabelPageResponse> ListLabelsAsync(Guid userId, Guid spaceId, bool unclaimed, string? after, CancellationToken cancellationToken);

    /// <summary>
    /// The label a scan or a typed code names, in whichever space it belongs to, once the person is known to be a
    /// member there. <paramref name="text"/> is the code or the whole scanned address (https://host/l/7K3Q).
    /// </summary>
    /// <exception cref="RuleViolationException">The text isn't a label code.</exception>
    /// <exception cref="NotFoundException">There is no such label, or it is in a space the person isn't in.</exception>
    Task<LabelResponse> FindLabelAsync(Guid userId, string text, CancellationToken cancellationToken);

    /// <summary>Makes a new item with an unclaimed label on it, in the container named. Writes a Created event.</summary>
    /// <exception cref="RuleViolationException">The label is already on something, or the item breaks a rule.</exception>
    Task<ItemResponse> ClaimLabelAsync(Guid userId, Guid spaceId, string code, ClaimLabelRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Sticks an unclaimed label on an existing item, freeing the label it had. Writes a Changed event. Attaching the
    /// label an item already has changes nothing.
    /// </summary>
    /// <exception cref="RuleViolationException">The label is already on another item.</exception>
    Task<ItemResponse> AttachLabelAsync(Guid userId, Guid spaceId, string code, AttachLabelRequest request, CancellationToken cancellationToken);
}
