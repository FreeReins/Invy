namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>The differences an audit walk found between a container and what Invy said was in it.</summary>
/// <param name="Container">The container, now marked seen.</param>
/// <param name="Seen">How many items were ticked as there.</param>
/// <param name="Missing">Its contents that weren't ticked, now marked missing (and any that already were). Lent items are left out.</param>
/// <param name="Found">Ticked items that were marked missing, now back to here.</param>
/// <param name="Elsewhere">Ticked items Invy has in another place. Nothing is moved: move them here if that's where they belong.</param>
public sealed record AuditResponse(
    ItemResponse Container,
    int Seen,
    IReadOnlyList<ItemSummary> Missing,
    IReadOnlyList<ItemSummary> Found,
    IReadOnlyList<ItemSummary> Elsewhere);
