namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item in a list, such as a container's contents.</summary>
/// <param name="Id">The item's id.</param>
/// <param name="Name">What it is called.</param>
/// <param name="Marking">Whatever is written on it, or null.</param>
/// <param name="TypeId">Its type, or null.</param>
/// <param name="HoldsItems">Whether other items can go inside it.</param>
/// <param name="Quantity">Null for a unique item; the amount of a counted one.</param>
/// <param name="Unit">The unit of a counted item, or null.</param>
/// <param name="Status">Whether it is here, missing, lent, disposed or archived.</param>
/// <param name="Contents">How many items are inside it, at any depth. Updated shortly after a change.</param>
/// <param name="Value">Its type's first money field times its amount (1 when unique), or null without one.</param>
/// <param name="ContentValue">The total value of what is inside it, at any depth. Currencies aren't converted.</param>
/// <param name="LabelCode">The code of the QR label stuck on it, such as 7K3Q, or null.</param>
public sealed record ItemSummary(
    Guid Id,
    string Name,
    string? Marking,
    Guid? TypeId,
    bool HoldsItems,
    decimal? Quantity,
    string? Unit,
    ItemStatus Status,
    int Contents,
    decimal? Value,
    decimal ContentValue,
    string? LabelCode);
