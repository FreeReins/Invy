using System.Text.Json;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A place, a container or a thing in a space.</summary>
internal sealed class Item
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SpaceId { get; set; }

    /// <summary>Its type, or null for none (yet).</summary>
    public Guid? TypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Whatever is written on the thing, such as ABC.</summary>
    public string? Marking { get; set; }

    /// <summary>The code of the QR label stuck on it, such as 7K3Q, or null. The Labels table says the same.</summary>
    public string? LabelCode { get; set; }

    /// <summary>The container it is in now, or null at the top.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>The ids of its containers and itself, top first, as an ltree (see <see cref="ItemPaths"/>).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>How many containers it is inside.</summary>
    public int Depth { get; set; }

    /// <summary>True when other items can go inside it.</summary>
    public bool HoldsItems { get; set; }

    /// <summary>Null for a unique item; the amount for a counted one.</summary>
    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    /// <summary>Field values by field id. Values of fields the type no longer has are kept.</summary>
    public Dictionary<Guid, JsonElement> Fields { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public string? Notes { get; set; }

    public ItemStatus Status { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Its type's first money field times its amount (1 when unique), or null without one.</summary>
    public decimal? Value { get; set; }

    /// <summary>How many current items are inside it, at any depth. Kept up to date by the rollups queue.</summary>
    public int ContentCount { get; set; }

    /// <summary>The total value of what is inside it, at any depth. Kept up to date by the rollups queue.</summary>
    public decimal ContentValue { get; set; }

    /// <summary>The type an AI model suggested for it while it was in the Inbox, or null for none (or ignored).</summary>
    public Guid? SuggestedTypeId { get; set; }

    /// <summary>When a model was last asked for its type, so the Inbox sweep asks about each item once.</summary>
    public DateTimeOffset? TypeSuggestedAt { get; set; }

    /// <summary>True for an example a starter pack added. Editing it makes it the person's own.</summary>
    public bool IsExample { get; set; }

    /// <summary>Goes up with each change; the database refuses a save from an older version.</summary>
    public int Version { get; set; }
}
