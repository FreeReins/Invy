namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>The part doesn't hold.</summary>
internal sealed record Not(QueryNode Part) : QueryNode;
