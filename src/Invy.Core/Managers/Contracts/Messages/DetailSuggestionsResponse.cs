using System.Text.Json;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Field values an AI model read from a photo of a label or nameplate, for a person to confirm.</summary>
/// <param name="Fields">Values for the item's type's fields, by field id. Empty when it could read none.</param>
/// <param name="Model">The name of the model that read it.</param>
public sealed record DetailSuggestionsResponse(IReadOnlyDictionary<Guid, JsonElement> Fields, string Model);
