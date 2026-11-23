namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Where to move an item.</summary>
/// <param name="ParentId">The container to put it in, or null to take it out to the top.</param>
public sealed record MoveItemRequest(Guid? ParentId);
