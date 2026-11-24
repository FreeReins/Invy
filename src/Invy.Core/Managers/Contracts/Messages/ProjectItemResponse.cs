namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Something a project sets aside.</summary>
/// <param name="Item">The item, as it is now.</param>
/// <param name="Amount">How much is set aside: 1 for a unique item.</param>
public sealed record ProjectItemResponse(ItemSummary Item, decimal Amount);
