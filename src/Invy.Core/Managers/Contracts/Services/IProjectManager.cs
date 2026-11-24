using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// Projects in a space: setting things aside for one, using them up, and finishing it. What a project sets aside
/// counts against low stock, but doesn't change an amount until it is used up.
/// </summary>
public interface IProjectManager
{
    /// <summary>The space's projects, open ones first, then by name; done ones only when asked for.</summary>
    Task<IReadOnlyList<ProjectResponse>> ListProjectsAsync(Guid userId, Guid spaceId, bool includeDone, CancellationToken cancellationToken);

    /// <summary>One project, with what it sets aside.</summary>
    Task<ProjectResponse> GetProjectAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>The open projects that set aside some of this item.</summary>
    Task<IReadOnlyList<ProjectResponse>> ListForItemAsync(Guid userId, Guid spaceId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>Starts a project.</summary>
    Task<ProjectResponse> CreateProjectAsync(Guid userId, Guid spaceId, ProjectRequest request, CancellationToken cancellationToken);

    /// <summary>Changes a project's name and notes.</summary>
    Task<ProjectResponse> UpdateProjectAsync(Guid userId, Guid spaceId, Guid id, ProjectRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Sets how much of an item the project sets aside. It can't set aside more than isn't already set aside for
    /// other projects. Alerts if that leaves the item low.
    /// </summary>
    Task<ProjectResponse> ReserveAsync(Guid userId, Guid spaceId, Guid id, ReserveRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Uses up what the project set aside: takes it off a counted item's amount (a QuantityChanged event), or disposes
    /// of a unique item (a Disposed event).
    /// </summary>
    Task<ProjectResponse> UseUpAsync(Guid userId, Guid spaceId, Guid id, UseUpRequest request, CancellationToken cancellationToken);

    /// <summary>Marks the project done and releases whatever it still sets aside.</summary>
    Task<ProjectResponse> FinishProjectAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>Removes the project, releasing whatever it still sets aside. Items used up stay used up.</summary>
    Task DeleteProjectAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);
}
