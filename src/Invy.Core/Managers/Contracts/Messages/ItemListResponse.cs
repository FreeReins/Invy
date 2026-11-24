namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A short list of items, such as what's running low or what's lent out.</summary>
/// <param name="Items">The items, in the list's own order.</param>
/// <param name="More">True when there were more than the list holds; search to see them all.</param>
public sealed record ItemListResponse(IReadOnlyList<ItemSummary> Items, bool More);
