namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>type: the item has one of these types.</summary>
internal sealed record OfType(IReadOnlyList<Guid> TypeIds) : QueryNode;
