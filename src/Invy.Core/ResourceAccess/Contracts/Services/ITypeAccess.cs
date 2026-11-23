using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>The stored item types of each space.</summary>
internal interface ITypeAccess
{
    /// <summary>A space's types, by name.</summary>
    Task<List<ItemType>> ListAsync(Guid spaceId, CancellationToken cancellationToken);

    /// <summary>One of a space's types, or null when the space has none with that id.</summary>
    Task<ItemType?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds the type, or changes the stored one. The change is refused on commit, with a
    /// <see cref="Managers.Contracts.ConflictException"/>, if someone else saved it since it was read.
    /// </summary>
    Task SaveAsync(ItemType type, CancellationToken cancellationToken);

    Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);
}
