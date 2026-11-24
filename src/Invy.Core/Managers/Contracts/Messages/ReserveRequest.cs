namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Sets how much of an item a project sets aside, replacing what it set aside before.</summary>
/// <param name="ItemId">The item.</param>
/// <param name="Amount">How much to set aside: 1 for a unique item, or 0 to set nothing aside.</param>
public sealed record ReserveRequest(Guid ItemId, decimal Amount);
