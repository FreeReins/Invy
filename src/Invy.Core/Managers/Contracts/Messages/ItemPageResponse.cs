namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One page of a list of items, by name.</summary>
/// <param name="Items">The items.</param>
/// <param name="Next">Pass as <c>after</c> for the next page; null on the last page.</param>
public sealed record ItemPageResponse(IReadOnlyList<ItemSummary> Items, string? Next);
