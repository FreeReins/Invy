namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ProjectItemEntity
{
    public Guid ProjectId { get; set; }

    public Guid SpaceId { get; set; }

    public Guid ItemId { get; set; }

    public decimal Amount { get; set; }
}
