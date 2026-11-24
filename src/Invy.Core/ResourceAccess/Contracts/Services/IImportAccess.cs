using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>A space's imports. Every call is filtered by space.</summary>
internal interface IImportAccess
{
    /// <summary>One import, or null when the space has none with that id.</summary>
    Task<Import?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>The space's imports, newest first, at most <paramref name="limit"/>.</summary>
    Task<List<Import>> ListAsync(Guid spaceId, int limit, CancellationToken cancellationToken);

    /// <summary>True when another import still needs the file at <paramref name="path"/> (the same file uploaded twice).</summary>
    Task<bool> FileInUseAsync(string path, Guid exceptId, CancellationToken cancellationToken);

    /// <summary>Adds the import, or changes the stored one.</summary>
    Task SaveAsync(Import import, CancellationToken cancellationToken);

    Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);
}
