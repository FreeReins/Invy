namespace Invy.Core.ResourceAccess.Persistence;

/// <summary>
/// Where an item sits: the ltree of its containers' ids and its own, and its depth. Kept apart from "Items" so moving a
/// container rewrites these narrow rows, with their one path index, rather than every column and index of the items
/// inside it (phase 7, docs/developers/design.md, Nesting and moves). Loaded and saved with its item.
/// </summary>
internal sealed class ItemPathEntity
{
    /// <summary>The item's id: the key, and the link to its item.</summary>
    public Guid Id { get; set; }

    /// <summary>An ltree column. See ItemPaths.</summary>
    public string Path { get; set; } = string.Empty;

    public int Depth { get; set; }
}
