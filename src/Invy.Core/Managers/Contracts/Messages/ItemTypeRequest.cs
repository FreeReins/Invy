namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item type as the editor saves it, whole: its name, look and fields.</summary>
/// <param name="Name">Its name, such as Tool or Box. Unique in the space.</param>
/// <param name="Icon">A short icon name, or null for the default.</param>
/// <param name="Colour">A colour such as <c>#3b82f6</c>, or null for the default.</param>
/// <param name="HoldsItems">True for places and containers, whose items can go inside them.</param>
/// <param name="Fields">The custom fields, in the order shown.</param>
/// <param name="Version">When changing a type: the version that was opened, so someone else's change isn't overwritten. Ignored when creating.</param>
public sealed record ItemTypeRequest(
    string Name, string? Icon, string? Colour, bool HoldsItems, IReadOnlyList<FieldDefinition> Fields, int Version);
