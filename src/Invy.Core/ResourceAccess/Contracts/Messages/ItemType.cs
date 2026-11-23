namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A kind of item in a space, such as Tool or Box, with its custom fields.</summary>
internal sealed class ItemType
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SpaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? Colour { get; set; }

    /// <summary>True for places and containers.</summary>
    public bool HoldsItems { get; set; }

    /// <summary>The custom fields, in the order shown. Stored as jsonb.</summary>
    public List<TypeField> Fields { get; set; } = [];

    /// <summary>Goes up with each change; the database refuses a save from an older version.</summary>
    public int Version { get; set; }
}
