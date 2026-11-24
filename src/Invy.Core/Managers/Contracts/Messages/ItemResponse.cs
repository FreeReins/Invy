using System.Text.Json;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item with all its details.</summary>
/// <param name="Id">The item's id.</param>
/// <param name="Name">What it is called.</param>
/// <param name="Marking">Whatever is written on it, or null.</param>
/// <param name="TypeId">Its type, or null.</param>
/// <param name="ParentId">The container it is in, or null at the top.</param>
/// <param name="HoldsItems">Whether other items can go inside it.</param>
/// <param name="Quantity">Null for a unique item; the amount of a counted one.</param>
/// <param name="Unit">The unit of a counted item, or null.</param>
/// <param name="Fields">Field values by field id, including fields the type no longer has.</param>
/// <param name="Tags">Its tags.</param>
/// <param name="Notes">Free text, or null.</param>
/// <param name="Status">Whether it is here, missing, lent, disposed or archived.</param>
/// <param name="LastSeenAt">When it was last seen, or null.</param>
/// <param name="CreatedAt">When it was added.</param>
/// <param name="UpdatedAt">When it last changed.</param>
/// <param name="Version">Goes up with each change. Send it back when saving.</param>
/// <param name="Value">Its type's first money field times its amount (1 when unique), or null without one.</param>
/// <param name="ContentCount">How many items are inside it, at any depth. Updated shortly after a change.</param>
/// <param name="ContentValue">The total value of what is inside it, at any depth. Currencies aren't converted.</param>
/// <param name="LabelCode">The code of the QR label stuck on it, such as 7K3Q, or null.</param>
/// <param name="SuggestedTypeId">While it is in the Inbox: the type an AI model suggested, to accept or ignore, or null.</param>
public sealed record ItemResponse(
    Guid Id,
    string Name,
    string? Marking,
    Guid? TypeId,
    Guid? ParentId,
    bool HoldsItems,
    decimal? Quantity,
    string? Unit,
    IReadOnlyDictionary<Guid, JsonElement> Fields,
    IReadOnlyList<string> Tags,
    string? Notes,
    ItemStatus Status,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version,
    decimal? Value,
    int ContentCount,
    decimal ContentValue,
    string? LabelCode,
    Guid? SuggestedTypeId = null);
