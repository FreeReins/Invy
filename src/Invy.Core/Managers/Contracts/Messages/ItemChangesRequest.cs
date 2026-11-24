namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// Some changes to an item, leaving everything not named as it is: for assistants and scripts, which needn't send the
/// whole item. Moving and changing the amount have their own calls.
/// </summary>
/// <param name="Name">A new name, or null to keep it.</param>
/// <param name="Marking">What's written on it; null keeps it, "" clears it.</param>
/// <param name="TypeId">A new type, or null to keep it.</param>
/// <param name="Notes">New notes; null keeps them, "" clears them.</param>
/// <param name="Tags">The tags it should have, replacing the old ones, or null to keep them.</param>
/// <param name="Fields">
/// Field values by the field's search name (such as brand or price), written as text ("Makita", "129.99",
/// "2024-03-01", "yes"). "" clears a field. Fields not named keep their values.
/// </param>
public sealed record ItemChangesRequest(
    string? Name = null,
    string? Marking = null,
    Guid? TypeId = null,
    string? Notes = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyDictionary<string, string>? Fields = null);
