using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>The stored spaces and their members.</summary>
internal interface ISpaceAccess
{
    /// <summary>A space with its members, or null when there is none with that id.</summary>
    Task<Space?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The spaces a person is a member of, oldest first.</summary>
    Task<List<Space>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The spaces with this feature switched on, by name, without their members. For scheduled work.</summary>
    Task<List<Space>> ListWithFeatureAsync(string feature, CancellationToken cancellationToken);

    /// <summary>Adds the space, or brings the stored one and its members in line with it.</summary>
    Task SaveAsync(Space space, CancellationToken cancellationToken);

    /// <summary>Removes a space. Everything in it goes with it, through the database's foreign keys.</summary>
    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}
