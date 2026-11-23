using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="IAttachmentManager"/>
internal sealed class AttachmentManager(
    IAttachmentAccess attachments,
    IFileStoreAccess files,
    IProductAccess web,
    IItemAccess items,
    IMessageAccess messages,
    ValidationEngine validation,
    ImageEngine images,
    TimeProvider clock) : IAttachmentManager
{
    public async Task<AttachmentResponse> AddAttachmentAsync(
        Guid userId, Guid spaceId, Guid itemId, AddAttachmentRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, itemId, cancellationToken);
        var (kind, contentType, extension) = ValidationEngine.AttachmentFile(request);
        var now = clock.GetUtcNow();

        // The file is written first: if the commit then fails, an unused file is left, never a row without its file.
        var stored = await files.SaveAsync(spaceId, request.Data, extension, cancellationToken);
        var attachment = new Attachment
        {
            SpaceId = spaceId,
            ItemId = item.Id,
            Kind = kind,
            Name = ValidationEngine.AttachmentName(request.Name, extension),
            ContentType = contentType,
            Path = stored.Path,
            Sha256 = stored.Sha256,
            Bytes = stored.Bytes,
            CreatedBy = userId,
            CreatedAt = now,
        };
        await attachments.SaveAsync(attachment, cancellationToken);
        if (kind == AttachmentKind.Photo)
        {
            await items.AddEventAsync(ItemEngine.Event(item, userId, ItemEventKind.PhotoAdded, now), cancellationToken);
            await messages.PublishAsync(new MakeThumbnail(spaceId, attachment.Id));
        }

        await messages.CommitAsync(cancellationToken);
        return attachment.ToResponse();
    }

    public async Task<AttachmentResponse> AttachFromWebAsync(
        Guid userId, Guid spaceId, Guid itemId, AttachFromWebRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        await validation.ItemAsync(spaceId, itemId, cancellationToken);
        var address = ValidationEngine.DownloadAddress(request.Url);
        var data = await web.DownloadAsync(address, ValidationEngine.MaxAttachmentBytes, cancellationToken);
        var name = request.Name ?? Uri.UnescapeDataString(address.Segments[^1]);
        return await AddAttachmentAsync(userId, spaceId, itemId, new AddAttachmentRequest(name, data), cancellationToken);
    }

    public async Task<IReadOnlyList<AttachmentResponse>> ListAttachmentsAsync(Guid userId, Guid spaceId, Guid itemId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        await validation.ItemAsync(spaceId, itemId, cancellationToken);
        return (await attachments.ListAsync(spaceId, itemId, cancellationToken)).Select(attachment => attachment.ToResponse()).ToList();
    }

    public async Task<AttachmentContent> OpenAttachmentAsync(Guid userId, Guid spaceId, Guid id, bool thumbnail, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var attachment = await validation.AttachmentAsync(spaceId, id, cancellationToken);
        if (thumbnail)
        {
            return attachment.HasThumbnail && files.OpenRead(Attachment.ThumbnailPath(attachment.Path)) is { } small
                ? new AttachmentContent(small, "image/jpeg", attachment.Name)
                : throw new NotFoundException($"{attachment.Name} has no thumbnail yet. Open the file itself.");
        }

        return files.OpenRead(attachment.Path) is { } content
            ? new AttachmentContent(content, attachment.ContentType, attachment.Name)
            : throw new NotFoundException($"The file for {attachment.Name} is missing from the files folder. Restore it from a backup, or attach it again.");
    }

    public async Task DeleteAttachmentAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var attachment = await validation.AttachmentAsync(spaceId, id, cancellationToken);
        var shared = await attachments.InUseAsync(spaceId, attachment.Path, attachment.Id, cancellationToken);
        await attachments.RemoveAsync(spaceId, id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        if (!shared)
        {
            // Only once the row is gone. Backups keep their own copy, so an older backup still restores it.
            files.Delete(attachment.Path);
            files.Delete(Attachment.ThumbnailPath(attachment.Path));
        }
    }

    public async Task MakeThumbnailAsync(MakeThumbnail message, CancellationToken cancellationToken)
    {
        if (await attachments.FindAsync(message.SpaceId, message.AttachmentId, cancellationToken) is not { Kind: AttachmentKind.Photo, HasThumbnail: false } attachment)
        {
            return;
        }

        var path = Attachment.ThumbnailPath(attachment.Path);
        if (!files.Exists(path))
        {
            if (await files.ReadAsync(attachment.Path, cancellationToken) is not { } original || images.Thumbnail(original) is not { } small)
            {
                return;
            }

            await files.WriteAsync(path, small, cancellationToken);
        }

        attachment.HasThumbnail = true;
        await attachments.SaveAsync(attachment, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }
}
