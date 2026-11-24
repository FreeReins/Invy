using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>The rows for files attached to items. Every call is filtered by space.</summary>
internal interface IAttachmentAccess
{
    /// <summary>One attachment, or null when the space has none with that id.</summary>
    Task<Attachment?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>An item's attachments, oldest first.</summary>
    Task<List<Attachment>> ListAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>For each of these items that has one, the id of its first photo with a thumbnail, in one query.</summary>
    Task<Dictionary<Guid, Guid>> FirstPhotosAsync(Guid spaceId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken);

    /// <summary>Adds the attachment, or changes the stored one.</summary>
    /// <summary>True when any item in the space that isn't an example has a photo.</summary>
    Task<bool> AnyPhotoAsync(Guid spaceId, CancellationToken cancellationToken);

    Task SaveAsync(Attachment attachment, CancellationToken cancellationToken);

    /// <summary>Removes an attachment's row. The file stays until nothing else points at it.</summary>
    Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>Whether any attachment in the space, other than <paramref name="exceptId"/>, points at this file.</summary>
    Task<bool> InUseAsync(Guid spaceId, string path, Guid exceptId, CancellationToken cancellationToken);
}
