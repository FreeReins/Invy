namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>is: and has: conditions.</summary>
internal sealed record Flag(ItemFlag Kind, IReadOnlyList<Guid> FieldIds) : QueryNode;
