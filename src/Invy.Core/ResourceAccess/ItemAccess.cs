using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="IItemAccess"/>
internal sealed partial class ItemAccess(InvyDbContext db) : IItemAccess
{
    public async Task<Item?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(spaceId, id, cancellationToken))?.ToContract();

    public async Task<List<Item>> FindManyAsync(Guid spaceId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await db.Items.Where(item => item.SpaceId == spaceId && ids.Contains(item.Id)).LoadAsync(cancellationToken);
        return db.Items.Local.Where(item => item.SpaceId == spaceId && ids.Contains(item.Id)).Select(item => item.ToContract()).ToList();
    }

    public Task<List<Item>> ListChildrenAsync(Guid spaceId, Guid? parentId, ItemCursor? after, int limit, CancellationToken cancellationToken)
    {
        var rows = Current(spaceId).Where(item => item.ParentId == parentId);
        if (parentId is null)
        {
            rows = rows.Where(item => item.TypeId != null || item.HoldsItems);
        }

        return PageAsync(rows, after, limit, cancellationToken);
    }

    public Task<List<Item>> ListInboxAsync(Guid spaceId, ItemCursor? after, int limit, CancellationToken cancellationToken) =>
        PageAsync(Current(spaceId).Where(item => item.ParentId == null && item.TypeId == null && !item.HoldsItems), after, limit, cancellationToken);

    public async Task<Dictionary<Guid, int>> CountChildrenAsync(Guid spaceId, IReadOnlyCollection<Guid> parentIds, CancellationToken cancellationToken) =>
        parentIds.Count == 0
            ? []
            : await Current(spaceId)
                .Where(item => item.ParentId != null && parentIds.Contains(item.ParentId.Value))
                .GroupBy(item => item.ParentId!.Value)
                .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

    public async Task SaveAsync(Item item, IReadOnlyList<ItemValue> values, CancellationToken cancellationToken)
    {
        var fresh = item.ToEntity();
        if (await TrackedAsync(item.SpaceId, item.Id, cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Items.Add(fresh);
        }

        // The sortable values are rewritten whole. Rows added earlier in this request are dropped from the context.
        await db.ItemValues.Where(value => value.ItemId == item.Id).LoadAsync(cancellationToken);
        foreach (var old in db.ItemValues.Local.Where(value => value.ItemId == item.Id).ToList())
        {
            db.ItemValues.Remove(old);
        }

        foreach (var value in values)
        {
            if (db.ItemValues.Local.FirstOrDefault(v => v.ItemId == item.Id && v.FieldId == value.FieldId) is { } removed)
            {
                // Same key as a row just marked for deletion: change that row instead.
                db.Entry(removed).State = EntityState.Modified;
                db.CopyInto(removed, value.ToEntity());
            }
            else
            {
                db.ItemValues.Add(value.ToEntity());
            }
        }
    }

    /// <summary>
    /// One UPDATE for the whole subtree: each path below the old one is cut at the moved item and put under the new
    /// one. It runs at once, so a transaction is opened first; the commit (MessageAccess) ends it with the rest of
    /// the request's changes, or a failure rolls it back.
    /// </summary>
    public async Task MoveContentsAsync(Guid spaceId, string oldPath, string newPath, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            await db.Database.BeginTransactionAsync(cancellationToken);
        }

        await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE "Items"
            SET "Path" = {newPath}::ltree || subpath("Path", nlevel({oldPath}::ltree)),
                "Depth" = nlevel({newPath}::ltree || subpath("Path", nlevel({oldPath}::ltree))) - 1
            WHERE "SpaceId" = {spaceId} AND "Path" <@ {oldPath}::ltree AND "Path" <> {oldPath}::ltree
            """,
            cancellationToken);
    }

    /// <summary>Items that are neither disposed nor archived.</summary>
    private IQueryable<ItemEntity> Current(Guid spaceId) =>
        db.Items.AsNoTracking().Where(item => item.SpaceId == spaceId && item.Status != ItemStatus.Disposed && item.Status != ItemStatus.Archived);

    private static async Task<List<Item>> PageAsync(IQueryable<ItemEntity> rows, ItemCursor? after, int limit, CancellationToken cancellationToken)
    {
        if (after is not null)
        {
            rows = rows.Where(item => string.Compare(item.Name, after.Name) > 0 || (item.Name == after.Name && item.Id.CompareTo(after.Id) > 0));
        }

        return (await rows.OrderBy(item => item.Name).ThenBy(item => item.Id).Take(limit).ToListAsync(cancellationToken))
            .Select(item => item.ToContract())
            .ToList();
    }

    /// <summary>The tracked row, including one added earlier in this request. Always filtered by space.</summary>
    private async Task<ItemEntity?> TrackedAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        db.Items.Local.FirstOrDefault(item => item.SpaceId == spaceId && item.Id == id)
        ?? await db.Items.FirstOrDefaultAsync(item => item.SpaceId == spaceId && item.Id == id, cancellationToken);
}
