using System.Text.Json;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One thing an AI model saw in a photo, to tick, edit and keep. Nothing is saved until it is added as an item.</summary>
/// <param name="Name">What it is, such as Baseball glove.</param>
/// <param name="TypeId">One of the space's types, or null when none fits.</param>
/// <param name="Quantity">The amount of a counted item, such as 3, or null for a unique one.</param>
/// <param name="Unit">The unit of a counted item, such as kg, or null.</param>
/// <param name="Fields">Values for the type's fields, by field id, each already the right kind of value.</param>
public sealed record ItemSuggestion(
    string Name, Guid? TypeId, decimal? Quantity, string? Unit, IReadOnlyDictionary<Guid, JsonElement> Fields);
