using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class AttachmentEntity
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public Guid ItemId { get; set; }

    public AttachmentKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public long Bytes { get; set; }

    public bool HasThumbnail { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
