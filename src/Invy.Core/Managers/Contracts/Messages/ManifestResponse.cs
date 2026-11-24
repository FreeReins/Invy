namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A packed box's manifest, to print and stick on it: where it goes and what's in it.</summary>
/// <param name="Box">The box.</param>
/// <param name="Destination">Where it goes, or null when it has no destination yet.</param>
/// <param name="DestinationPath">The destination's containers and itself, top first, such as New house › Kitchen.</param>
/// <param name="Contents">What is directly inside it, by name. A box inside it shows how many things it holds.</param>
/// <param name="More">True when there was more inside than a page holds.</param>
public sealed record ManifestResponse(
    ItemSummary Box, ItemRef? Destination, IReadOnlyList<ItemRef> DestinationPath, IReadOnlyList<ItemSummary> Contents, bool More);
