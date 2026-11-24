namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What an AI model saw in a photo, for a person to confirm.</summary>
/// <param name="Items">The things it saw, in the order it listed them. Empty when it saw nothing it could name.</param>
/// <param name="Model">The name of the model that looked, such as Claude.</param>
public sealed record ItemSuggestionsResponse(IReadOnlyList<ItemSuggestion> Items, string Model);
