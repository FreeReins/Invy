namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A file attached to an item. Its bytes are at /api/spaces/{spaceId}/attachments/{id}/file.</summary>
/// <param name="Id">The attachment's id.</param>
/// <param name="ItemId">The item it is attached to.</param>
/// <param name="Kind">A photo or a document.</param>
/// <param name="Name">The file's name.</param>
/// <param name="ContentType">Its media type, such as image/jpeg, worked out from its bytes.</param>
/// <param name="Bytes">Its size.</param>
/// <param name="HasThumbnail">Whether a small copy is ready at .../thumbnail. Photos get one shortly after they are added.</param>
/// <param name="CreatedAt">When it was attached.</param>
public sealed record AttachmentResponse(
    Guid Id,
    Guid ItemId,
    AttachmentKind Kind,
    string Name,
    string ContentType,
    long Bytes,
    bool HasThumbnail,
    DateTimeOffset CreatedAt);
