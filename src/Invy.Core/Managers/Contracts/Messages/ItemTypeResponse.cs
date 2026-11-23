namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item type in a space.</summary>
/// <param name="Id">The type's id.</param>
/// <param name="Name">Its name, such as Tool or Box.</param>
/// <param name="Icon">A short icon name, or null for the default.</param>
/// <param name="Colour">A colour such as <c>#3b82f6</c>, or null for the default.</param>
/// <param name="HoldsItems">True for places and containers.</param>
/// <param name="Fields">The custom fields, in the order shown.</param>
/// <param name="Version">Goes up with each change. Send it back when saving.</param>
public sealed record ItemTypeResponse(
    Guid Id, string Name, string? Icon, string? Colour, bool HoldsItems, IReadOnlyList<FieldDefinition> Fields, int Version);
