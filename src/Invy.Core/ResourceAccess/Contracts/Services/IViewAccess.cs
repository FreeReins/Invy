using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>The stored views: saved searches, personal or shared with a space.</summary>
internal interface IViewAccess
{
    /// <summary>A view in this space, or null when there is none with that id.</summary>
    Task<View?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>The space's shared views and the person's own, pinned first, then by name.</summary>
    Task<List<View>> ListAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>How many views the person has made in the space.</summary>
    Task<int> CountOwnedAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Adds the view, or brings the stored one in line with it.</summary>
    Task SaveAsync(View view, CancellationToken cancellationToken);

    /// <summary>Removes a view.</summary>
    Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);
}
