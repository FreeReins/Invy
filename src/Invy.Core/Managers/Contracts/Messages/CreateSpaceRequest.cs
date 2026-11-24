namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A new space. The person making it becomes its owner.</summary>
/// <param name="Name">Its name, such as Home or Workshop.</param>
/// <param name="Preset">Its kind: a starter pack to begin with (home, hobby, collection, business), or null for none.</param>
/// <param name="Features">The features to switch on, or null for the kind's defaults.</param>
/// <param name="Places">Names of places to make at the top, as containers of the kind's place type, or null for none.</param>
/// <param name="Examples">True to add the kind's example items, which can be removed later in one go.</param>
public sealed record CreateSpaceRequest(
    string Name,
    string? Preset,
    IReadOnlyList<string>? Features = null,
    IReadOnlyList<string>? Places = null,
    bool Examples = false);
