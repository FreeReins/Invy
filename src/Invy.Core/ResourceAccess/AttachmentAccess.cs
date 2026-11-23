using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="IAttachmentAccess"/>
internal sealed class AttachmentAccess(InvyDbContext db) : IAttachmentAccess
{
    public async Task<Attachment?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(spaceId, id, cancellationToken))?.ToContract();

    public async Task<List<Attachment>> ListAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken) =>
        (await db.Attachments.AsNoTracking()
            .Where(attachment => attachment.SpaceId == spaceId && attachment.ItemId == itemId)
            .OrderBy(attachment => attachment.CreatedAt).ThenBy(attachment => attachment.Id)
            .ToListAsync(cancellationToken))
        .Select(attachment => attachment.ToContract())
        .ToList();

    public async Task SaveAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        var fresh = attachment.ToEntity();
        if (await TrackedAsync(attachment.SpaceId, attachment.Id, cancellationToken) is { } tracked)
        {
            db.Entry(tracked).CurrentValues.SetValues(fresh);
        }
        else
        {
            db.Attachments.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(spaceId, id, cancellationToken) is { } tracked)
        {
            db.Attachments.Remove(tracked);
        }
    }

    public Task<bool> InUseAsync(Guid spaceId, string path, Guid exceptId, CancellationToken cancellationToken) =>
        db.Attachments.AnyAsync(attachment => attachment.SpaceId == spaceId && attachment.Path == path && attachment.Id != exceptId, cancellationToken);

    /// <summary>The tracked row, including one added earlier in this request. Always filtered by space.</summary>
    private async Task<AttachmentEntity?> TrackedAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        db.Attachments.Local.FirstOrDefault(attachment => attachment.SpaceId == spaceId && attachment.Id == id)
        ?? await db.Attachments.FirstOrDefaultAsync(attachment => attachment.SpaceId == spaceId && attachment.Id == id, cancellationToken);
}
