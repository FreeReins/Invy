namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Several items to put in one container, each with everything inside it.</summary>
/// <param name="ItemIds">The items to move, 1 to 100.</param>
/// <param name="ParentId">The container to put them in, or null to take them out to the top.</param>
public sealed record MoveItemsRequest(IReadOnlyList<Guid> ItemIds, Guid? ParentId);
