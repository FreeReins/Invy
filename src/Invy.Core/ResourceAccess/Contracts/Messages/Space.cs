namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A space, such as Home or Workshop: its own items, types, views and labels, and the people in it.</summary>
internal sealed class Space
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    /// <summary>The starter pack of types it was made with (home, hobby, business), or null for none.</summary>
    public string? Preset { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Everyone who can open the space, with their role.</summary>
    public List<SpaceMember> Members { get; set; } = [];
}
