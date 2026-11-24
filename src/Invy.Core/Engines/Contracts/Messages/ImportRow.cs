namespace Invy.Core.Engines.Contracts.Messages;

/// <summary>One row of an import's file, read through its column mapping. Field values are still text.</summary>
/// <param name="Name">What it is called.</param>
/// <param name="Marking">Whatever is written on it, or null.</param>
/// <param name="Quantity">The amount of a counted item, or null for a unique one.</param>
/// <param name="Unit">The unit of a counted item, or null.</param>
/// <param name="Tags">Its tags, lower case.</param>
/// <param name="Notes">Its notes, with any "Column: value" lines, or null.</param>
/// <param name="Location">The containers it is in, outermost first; empty to go straight into the import's container.</param>
/// <param name="TypeName">The type it names, or null to use the import's type.</param>
/// <param name="Fields">Field values as text, by field id.</param>
internal sealed record ImportRow(
    string Name,
    string? Marking,
    decimal? Quantity,
    string? Unit,
    IReadOnlyList<string> Tags,
    string? Notes,
    IReadOnlyList<string> Location,
    string? TypeName,
    IReadOnlyDictionary<Guid, string> Fields);
