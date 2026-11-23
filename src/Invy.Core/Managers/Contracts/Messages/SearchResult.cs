namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One item found by a search, with where it is.</summary>
/// <param name="Item">The item.</param>
/// <param name="Chain">The containers it is in, top first.</param>
public sealed record SearchResult(ItemSummary Item, IReadOnlyList<ItemRef> Chain);
