namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A starter pack: a kind of space, with the item types, features and places it begins with.</summary>
/// <param name="Key">What to send as <see cref="CreateSpaceRequest.Preset"/>, such as home.</param>
/// <param name="Name">The name shown, such as Home.</param>
/// <param name="Description">One sentence on what it holds.</param>
/// <param name="Types">The names of its types, in order.</param>
/// <param name="Icon">A generic icon name for its card, such as house, or null.</param>
/// <param name="Features">The features a space of this kind starts with.</param>
/// <param name="Places">The places setup suggests, or null when it suggests none.</param>
/// <param name="Examples">The names of the example items it can add at the top, in order. Empty when it has none.</param>
/// <param name="GettingStarted">The keys of the steps on its getting-started card, in order.</param>
/// <param name="Hints">Text for empty screens by tab (find, browse, scan, types). A tab without one uses general text.</param>
public sealed record StarterPackResponse(
    string Key,
    string Name,
    string Description,
    IReadOnlyList<string> Types,
    string? Icon,
    IReadOnlyList<string> Features,
    StarterPlacesResponse? Places,
    IReadOnlyList<string> Examples,
    IReadOnlyList<string> GettingStarted,
    IReadOnlyDictionary<string, string> Hints);
