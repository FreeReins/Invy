namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A search, as QueryEngine reads it from the text people type: a tree of conditions with every name already turned
/// into ids. ItemAccess turns it into SQL with parameters; no text typed by anyone becomes SQL.
/// </summary>
/// <param name="Filter">The conditions every result meets.</param>
/// <param name="IncludeGone">True when disposed and archived items were asked for (is:disposed); otherwise left out.</param>
/// <param name="Words">The plain words searched for, to rank results by.</param>
/// <param name="Sort">The order of the results.</param>
internal sealed record ItemQuery(QueryNode Filter, bool IncludeGone, IReadOnlyList<string> Words, SearchOrder Sort);
