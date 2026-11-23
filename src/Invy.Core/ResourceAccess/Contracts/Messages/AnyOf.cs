namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>At least one part holds.</summary>
internal sealed record AnyOf(IReadOnlyList<QueryNode> Parts) : QueryNode;
