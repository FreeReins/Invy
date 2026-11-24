using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ViewEntity
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public ViewDisplay Display { get; set; }

    /// <summary>A text[] column.</summary>
    public List<string> Columns { get; set; } = [];

    public string? Sort { get; set; }

    public string? GroupBy { get; set; }

    public bool Shared { get; set; }

    public bool Pinned { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
