namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A space's new kind. It changes only the hints and getting-started steps, never the types.</summary>
/// <param name="Kind">A starter pack's key (home, hobby, collection, business), or null for none.</param>
public sealed record SpaceKindRequest(string? Kind);
