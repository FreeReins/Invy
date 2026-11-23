namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class SpaceEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Preset { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<SpaceMemberEntity> Members { get; set; } = [];
}
