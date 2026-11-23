namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ItemTypeEntity
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? Colour { get; set; }

    public bool HoldsItems { get; set; }

    /// <summary>The field definitions as JSON, in a jsonb column.</summary>
    public string Fields { get; set; } = "[]";

    public int Version { get; set; }
}
