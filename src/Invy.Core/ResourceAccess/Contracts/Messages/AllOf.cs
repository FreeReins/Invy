namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>Every part holds. No parts: everything matches.</summary>
internal sealed record AllOf(IReadOnlyList<QueryNode> Parts) : QueryNode;
