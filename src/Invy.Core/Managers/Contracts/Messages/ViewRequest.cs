namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A view to make or save: a search plus how to show it.</summary>
/// <param name="Name">What it is called, such as "Not seen in a year".</param>
/// <param name="Query">The search, written as in the search box (docs/developers/design.md, Search). Read again each time the view runs, so <c>seen&gt;1y</c> always counts back from today.</param>
/// <param name="Display">How the results are shown.</param>
/// <param name="Columns">For a table: the columns, in order. Each is one of name, type, place, quantity, value, seen, added, label, or a field's search name. Empty for the default (name, type, place).</param>
/// <param name="Sort">relevance, name or recent; null for relevance with words and name without.</param>
/// <param name="GroupBy">For a board: the search name of the select field to group by. Ignored for other displays.</param>
/// <param name="Shared">True to share it with everyone in the space (editors and owners only); false to keep it to yourself.</param>
/// <param name="Pinned">True to show it on Find. A pinned shared view shows there for everyone.</param>
public sealed record ViewRequest(
    string Name, string? Query, ViewDisplay Display, IReadOnlyList<string>? Columns, string? Sort, string? GroupBy, bool Shared, bool Pinned);
