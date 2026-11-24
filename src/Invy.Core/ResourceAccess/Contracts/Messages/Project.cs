using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A project in a space, such as Build a bookshelf, with what it has set aside.</summary>
internal sealed class Project
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SpaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ProjectStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Goes up with each change; the database refuses a save from an older version.</summary>
    public int Version { get; set; }

    /// <summary>What it has set aside. The items' reserved totals add these up.</summary>
    public List<ProjectItem> Items { get; set; } = [];
}
