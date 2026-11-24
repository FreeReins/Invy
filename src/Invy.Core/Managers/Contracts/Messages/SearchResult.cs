namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One item found by a search, with where it is.</summary>
/// <param name="Item">The item.</param>
/// <param name="Chain">The containers it is in, top first.</param>
/// <param name="Details">When a view runs: its field values, dates and photo, for the view's columns, cards and board. Null in a search.</param>
public sealed record SearchResult(ItemSummary Item, IReadOnlyList<ItemRef> Chain, ItemDetails? Details = null);
