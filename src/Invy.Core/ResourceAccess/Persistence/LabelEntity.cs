namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class LabelEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid SpaceId { get; set; }

    public Guid? ItemId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? PrintedAt { get; set; }
}
