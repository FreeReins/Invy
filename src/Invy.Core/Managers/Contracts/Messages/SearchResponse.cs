namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A page of search results.</summary>
/// <param name="Results">The items found, best match first (or in the order asked for).</param>
/// <param name="Total">How many items match, on the first page only; null on later pages.</param>
/// <param name="TotalIsEstimate">True when there are more than 10,000 and <paramref name="Total"/> is an estimate.</param>
/// <param name="Next">Pass as <c>after</c> for the next page; null on the last page.</param>
public sealed record SearchResponse(IReadOnlyList<SearchResult> Results, int? Total, bool TotalIsEstimate, string? Next);
