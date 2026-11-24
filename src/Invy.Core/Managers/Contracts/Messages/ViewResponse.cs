namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A saved view.</summary>
/// <param name="Id">The view's id.</param>
/// <param name="Name">What it is called.</param>
/// <param name="Query">The search, as typed.</param>
/// <param name="Display">How the results are shown.</param>
/// <param name="Columns">For a table: the columns, in order.</param>
/// <param name="Sort">relevance, name or recent; null for the default.</param>
/// <param name="GroupBy">For a board: the select field's search name; otherwise null.</param>
/// <param name="Shared">True when everyone in the space sees it.</param>
/// <param name="Pinned">True when it shows on Find.</param>
/// <param name="Mine">True when the person asking made it.</param>
/// <param name="CanChange">True when the person asking can change or remove it.</param>
/// <param name="UpdatedAt">When it was last saved.</param>
public sealed record ViewResponse(
    Guid Id, string Name, string Query, ViewDisplay Display, IReadOnlyList<string> Columns, string? Sort, string? GroupBy,
    bool Shared, bool Pinned, bool Mine, bool CanChange, DateTimeOffset UpdatedAt);
