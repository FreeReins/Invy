using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Attachments.</content>
internal static partial class Mapping
{
    public static AttachmentResponse ToResponse(this Attachment attachment) => new(
        attachment.Id, attachment.ItemId, attachment.Kind, attachment.Name, attachment.ContentType, attachment.Bytes,
        attachment.HasThumbnail, attachment.CreatedAt);
}
