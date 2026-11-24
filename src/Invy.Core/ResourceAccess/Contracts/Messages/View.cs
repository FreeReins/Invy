using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A saved search and how to show it, kept by one person or shared with the space.</summary>
internal sealed class View
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SpaceId { get; set; }

    /// <summary>The person who made it. Only they see it unless it is shared.</summary>
    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>The search text, read again by QueryEngine each time the view runs.</summary>
    public string Query { get; set; } = string.Empty;

    public ViewDisplay Display { get; set; }

    public List<string> Columns { get; set; } = [];

    public string? Sort { get; set; }

    public string? GroupBy { get; set; }

    public bool Shared { get; set; }

    public bool Pinned { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
