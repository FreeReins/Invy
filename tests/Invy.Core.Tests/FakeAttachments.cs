using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Attachment rows kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed class FakeAttachments : IAttachmentAccess
{
    public List<Attachment> Rows { get; } = [];

    /// <summary>Whether an item is an example; <see cref="TestCore"/> points it at its items.</summary>
    public Func<Guid, bool> IsExample { get; set; } = _ => false;

    public Task<bool> AnyPhotoAsync(Guid spaceId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Any(row => row.SpaceId == spaceId && row.Kind == AttachmentKind.Photo && !IsExample(row.ItemId)));

    public Task<Attachment?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(row => row.SpaceId == spaceId && row.Id == id) is { } row ? Copy(row) : null);

    public Task<List<Attachment>> ListAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(row => row.SpaceId == spaceId && row.ItemId == itemId).OrderBy(row => row.CreatedAt).ThenBy(row => row.Id).Select(Copy).ToList());

    public Task SaveAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == attachment.Id);
        Rows.Add(Copy(attachment));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.SpaceId == spaceId && row.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> InUseAsync(Guid spaceId, string path, Guid exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Any(row => row.SpaceId == spaceId && row.Path == path && row.Id != exceptId));

    private static Attachment Copy(Attachment row) => new()
    {
        Id = row.Id,
        SpaceId = row.SpaceId,
        ItemId = row.ItemId,
        Kind = row.Kind,
        Name = row.Name,
        ContentType = row.ContentType,
        Path = row.Path,
        Sha256 = row.Sha256,
        Bytes = row.Bytes,
        HasThumbnail = row.HasThumbnail,
        CreatedBy = row.CreatedBy,
        CreatedAt = row.CreatedAt,
    };
}
