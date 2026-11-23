using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// Photos and PDFs attached to items, kept as files under Data:Root/files and backed up with the database. Viewers look
/// and download; editors and owners attach and remove.
/// </summary>
public interface IAttachmentManager
{
    /// <summary>
    /// Attaches a file to an item. A photo writes a PhotoAdded event and has its thumbnail made shortly after, on the
    /// images queue.
    /// </summary>
    /// <exception cref="RuleViolationException">The file is empty, too big, or not a photo or PDF.</exception>
    Task<AttachmentResponse> AddAttachmentAsync(Guid userId, Guid spaceId, Guid itemId, AddAttachmentRequest request, CancellationToken cancellationToken);

    /// <summary>Downloads a photo or PDF from the public internet and attaches it, as <see cref="AddAttachmentAsync"/> does.</summary>
    /// <exception cref="WebSourceException">It couldn't be downloaded, or the address is on a private network.</exception>
    Task<AttachmentResponse> AttachFromWebAsync(Guid userId, Guid spaceId, Guid itemId, AttachFromWebRequest request, CancellationToken cancellationToken);

    /// <summary>An item's attachments, oldest first.</summary>
    Task<IReadOnlyList<AttachmentResponse>> ListAttachmentsAsync(Guid userId, Guid spaceId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>An attachment's file, or its thumbnail when <paramref name="thumbnail"/> is true.</summary>
    /// <exception cref="NotFoundException">It no longer exists, or has no thumbnail yet.</exception>
    Task<AttachmentContent> OpenAttachmentAsync(Guid userId, Guid spaceId, Guid id, bool thumbnail, CancellationToken cancellationToken);

    /// <summary>Removes an attachment. Its file goes too, unless another attachment in the space is the same file.</summary>
    Task DeleteAttachmentAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>Makes a photo's thumbnail. Safe to run twice; a file that can't be read as a picture gets none.</summary>
    Task MakeThumbnailAsync(MakeThumbnail message, CancellationToken cancellationToken);
}
