namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A QR label: a short code made before anything exists, claimed by the item it is stuck on.</summary>
internal sealed class Label
{
    /// <summary>The short code, such as 7K3Q. Unique across the whole install, so a scan finds its space.</summary>
    public string Code { get; set; } = string.Empty;

    public Guid SpaceId { get; set; }

    /// <summary>The item it is on, or null until it is claimed.</summary>
    public Guid? ItemId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it was last printed on a sheet, or null if never.</summary>
    public DateTimeOffset? PrintedAt { get; set; }
}
