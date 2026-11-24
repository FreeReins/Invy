namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class SpaceEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Preset { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>A text[] column.</summary>
    public List<string> Features { get; set; } = [];

    public List<SpaceMemberEntity> Members { get; set; } = [];
}
