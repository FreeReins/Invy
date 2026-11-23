namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>qty: a counted item's amount is in the range.</summary>
internal sealed record QuantityIn(Range<decimal> Range) : QueryNode;
