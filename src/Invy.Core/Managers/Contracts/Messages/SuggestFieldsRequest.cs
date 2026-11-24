namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A type to suggest fields for.</summary>
/// <param name="Name">The type's name, such as Lego sets.</param>
/// <param name="ExistingKeys">The search names of fields it already has, so they aren't suggested again.</param>
public sealed record SuggestFieldsRequest(string Name, IReadOnlyList<string>? ExistingKeys = null);
