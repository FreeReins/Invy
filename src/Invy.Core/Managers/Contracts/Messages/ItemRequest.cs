using System.Text.Json;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item as the form saves it, whole. Moving and changing the amount have their own calls.</summary>
/// <param name="Name">What it is called, such as Baseball or Box F.</param>
/// <param name="Marking">Whatever is written on it, such as ABC, or null.</param>
/// <param name="TypeId">Its type, or null for none.</param>
/// <param name="ParentId">When adding: the container to put it in, or null for the top (or the Inbox). Ignored when changing; use move.</param>
/// <param name="HoldsItems">Whether other items can go inside it. Null takes the type's setting.</param>
/// <param name="Quantity">When adding: null for a unique item, or the amount of a counted one. When changing: switches between unique and counted, or sets the amount.</param>
/// <param name="Unit">The unit of a counted item, such as kg, or null.</param>
/// <param name="Fields">Field values by field id. A field left out is cleared; values of fields the type no longer has must be sent back unchanged to keep them.</param>
/// <param name="Tags">Tags, such as garden.</param>
/// <param name="Notes">Free text.</param>
/// <param name="Version">When changing: the version that was opened. Ignored when adding.</param>
public sealed record ItemRequest(
    string Name,
    string? Marking,
    Guid? TypeId,
    Guid? ParentId,
    bool? HoldsItems,
    decimal? Quantity,
    string? Unit,
    IReadOnlyDictionary<Guid, JsonElement>? Fields,
    IReadOnlyList<string>? Tags,
    string? Notes,
    int Version);
