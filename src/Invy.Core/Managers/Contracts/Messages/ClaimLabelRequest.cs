namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Makes a new item for an unclaimed label, with the label on it.</summary>
/// <param name="ParentId">The container to put it in (the sticky place), or null for the top.</param>
/// <param name="Name">What to call it, or null for "Box" and the code, such as Box 7K3Q.</param>
/// <param name="TypeId">Its type, or null for none. Without a type it holds items, like a box.</param>
public sealed record ClaimLabelRequest(Guid? ParentId, string? Name, Guid? TypeId);
