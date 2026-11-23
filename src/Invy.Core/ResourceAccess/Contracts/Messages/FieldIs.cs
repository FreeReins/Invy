namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary><c>field:value</c>: one of the fields (from any type with that search name) has the value.</summary>
internal sealed record FieldIs(IReadOnlyList<FieldMatch> Matches) : QueryNode;
