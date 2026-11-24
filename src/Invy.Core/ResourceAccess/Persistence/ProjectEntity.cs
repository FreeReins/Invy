using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ProjectEntity
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ProjectStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int Version { get; set; }

    public List<ProjectItemEntity> Items { get; set; } = [];
}
