namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Sets or clears a counted item's low-stock level, leaving the rest of it as it is.</summary>
/// <param name="At">The amount at or below which it is low, or null for no level.</param>
/// <param name="Version">The version that was opened, or null to change whatever is there now.</param>
public sealed record LowStockRequest(decimal? At, int? Version = null);
