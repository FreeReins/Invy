using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ItemEntity
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public Guid? TypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Marking { get; set; }

    public string? LabelCode { get; set; }

    public Guid? ParentId { get; set; }

    /// <summary>An ltree column. See ItemPaths.</summary>
    public string Path { get; set; } = string.Empty;

    public int Depth { get; set; }

    public bool HoldsItems { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

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

    public int Version { get; set; }
}
