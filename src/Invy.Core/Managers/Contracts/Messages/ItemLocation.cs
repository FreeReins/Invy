namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Where one item is, in words: the answer to "where is my baseball?".</summary>
/// <param name="Id">The item's id.</param>
/// <param name="Name">Its name, with its marking when it has one, such as Box (F).</param>
/// <param name="Where">The containers it is in, top first, such as "Garage › Shelf 2 › Box F", "the top of the space", or "the Inbox (no place yet)".</param>
/// <param name="Status">Whether it is here, missing, lent, disposed or archived.</param>
/// <param name="Amount">A counted item's amount, such as "3.5 kg", or null for a unique one.</param>
/// <param name="LastSeenAt">When it was last seen where Invy says it is, or null.</param>
public sealed record ItemLocation(Guid Id, string Name, string Where, ItemStatus Status, string? Amount, DateTimeOffset? LastSeenAt);
