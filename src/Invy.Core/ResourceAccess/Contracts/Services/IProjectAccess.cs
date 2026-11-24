using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>A space's projects, with what each has set aside. Every call is filtered by space.</summary>
internal interface IProjectAccess
{
    /// <summary>One project with its reservations, or null when the space has none with that id.</summary>
    Task<Project?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>The space's projects with their reservations, open ones first, then by name.</summary>
    Task<List<Project>> ListAsync(Guid spaceId, bool includeDone, CancellationToken cancellationToken);

    /// <summary>The open projects that set aside some of this item, by name.</summary>
    Task<List<Project>> ListForItemAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>True when the space has a project, open or done.</summary>
    Task<bool> AnyAsync(Guid spaceId, CancellationToken cancellationToken);

    /// <summary>Adds the project, or brings the stored one and its reservations in line with it.</summary>
    Task SaveAsync(Project project, CancellationToken cancellationToken);

    /// <summary>Removes a project and its reservations. The items' reserved totals are the caller's to release.</summary>
    Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);
}
