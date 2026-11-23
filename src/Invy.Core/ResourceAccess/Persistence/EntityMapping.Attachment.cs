using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies AttachmentEntity to and from Attachment.</content>
internal static partial class EntityMapping
{
    public static Attachment ToContract(this AttachmentEntity entity) => new()
    {
        Id = entity.Id,
        SpaceId = entity.SpaceId,
        ItemId = entity.ItemId,
        Kind = entity.Kind,
        Name = entity.Name,
        ContentType = entity.ContentType,
        Path = entity.Path,
        Sha256 = entity.Sha256,
        Bytes = entity.Bytes,
        HasThumbnail = entity.HasThumbnail,
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt,
    };

    public static AttachmentEntity ToEntity(this Attachment attachment) => new()
    {
        Id = attachment.Id,
        SpaceId = attachment.SpaceId,
        ItemId = attachment.ItemId,
        Kind = attachment.Kind,
        Name = attachment.Name,
        ContentType = attachment.ContentType,
        Path = attachment.Path,
        Sha256 = attachment.Sha256,
        Bytes = attachment.Bytes,
        HasThumbnail = attachment.HasThumbnail,
        CreatedBy = attachment.CreatedBy,
        CreatedAt = attachment.CreatedAt,
    };
}
