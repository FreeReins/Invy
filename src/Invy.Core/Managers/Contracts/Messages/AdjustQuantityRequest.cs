namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A change to a counted item's amount.</summary>
/// <param name="Delta">How much to add, or (negative) take away. The amount can't go below zero.</param>
public sealed record AdjustQuantityRequest(decimal Delta);
