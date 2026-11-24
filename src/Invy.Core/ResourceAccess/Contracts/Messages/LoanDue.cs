namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary><c>is:overdue</c>, <c>due&lt;7d</c>: it is lent out and due back within the range.</summary>
internal sealed record LoanDue(Range<DateOnly> Range) : QueryNode;
