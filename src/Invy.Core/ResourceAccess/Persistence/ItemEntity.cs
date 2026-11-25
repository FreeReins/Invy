using System.Text.Json.Serialization;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ItemEntity
{
    /// <summary>The item's id, also set on its <see cref="Place"/>.</summary>
    public Guid Id
    {
        get;
        set
        {
            field = value;
            Place.Id = value;
        }
    }

    public Guid SpaceId { get; set; }

    public Guid? TypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Marking { get; set; }

    public string? LabelCode { get; set; }

    public Guid? ParentId { get; set; }

    /// <summary>Its row in "ItemPaths": its path and depth. Backups write those as <see cref="Path"/> and <see cref="Depth"/>.</summary>
    [JsonIgnore]
    public ItemPathEntity Place { get; set; } = new();

    /// <summary>The ltree of its containers' ids and its own, stored in <see cref="Place"/>. See ItemPaths.</summary>
    public string Path
    {
        get => Place.Path;
        set => Place.Path = value;
    }

    /// <summary>How many containers it is inside, stored in <see cref="Place"/>.</summary>
    public int Depth
    {
        get => Place.Depth;
        set => Place.Depth = value;
    }

    public bool HoldsItems { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public decimal? LowStockAt { get; set; }

    public decimal Reserved { get; set; }

    public string? LentTo { get; set; }

    public DateTimeOffset? LentAt { get; set; }

    public DateOnly? DueOn { get; set; }

    public Guid? DestinationId { get; set; }

    /// <summary>Field values by field id, as JSON in a jsonb column.</summary>
    public string Fields { get; set; } = "{}";

    public List<string> Tags { get; set; } = [];

    public string? Notes { get; set; }

    public ItemStatus Status { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public decimal? Value { get; set; }

    public int ContentCount { get; set; }

    public decimal ContentValue { get; set; }

    public Guid? SuggestedTypeId { get; set; }

    public DateTimeOffset? TypeSuggestedAt { get; set; }

    public bool IsExample { get; set; }

    public int Version { get; set; }
}
