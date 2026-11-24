namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Fields an AI model suggested for a type, for a person to pick from. Nothing is saved until the type is.</summary>
/// <param name="Fields">The suggested fields, each with a null id and a search name no other field has.</param>
/// <param name="Model">The name of the model that suggested them.</param>
public sealed record FieldSuggestionsResponse(IReadOnlyList<FieldDefinition> Fields, string Model);
