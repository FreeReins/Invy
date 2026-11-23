namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary><c>field&gt;n</c>, <c>field:a..b</c>: a sortable number, money or date field is in the range.</summary>
internal sealed record FieldRange(IReadOnlyList<Guid> FieldIds, Range<decimal>? Number, Range<DateOnly>? Date) : QueryNode;
