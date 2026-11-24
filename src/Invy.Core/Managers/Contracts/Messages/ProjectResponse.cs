namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A project and what it sets aside.</summary>
/// <param name="Id">The project's id.</param>
/// <param name="Name">What it is.</param>
/// <param name="Notes">Free text, or null.</param>
/// <param name="Status">Open, or Done (holding nothing back).</param>
/// <param name="CreatedAt">When it was started.</param>
/// <param name="UpdatedAt">When it last changed.</param>
/// <param name="Version">Goes up with each change. Send it back when changing it.</param>
/// <param name="Items">What it sets aside, by name.</param>
public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Notes,
    ProjectStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version,
    IReadOnlyList<ProjectItemResponse> Items);
