using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="IViewAccess"/>
internal sealed class ViewAccess(InvyDbContext db) : IViewAccess
{
    public async Task<View?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(spaceId, id, cancellationToken))?.ToContract();

    public async Task<List<View>> ListAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken) =>
        (await db.Views.AsNoTracking()
            .Where(view => view.SpaceId == spaceId && (view.Shared || view.OwnerId == userId))
            .OrderByDescending(view => view.Pinned).ThenBy(view => view.Name).ThenBy(view => view.Id)
            .ToListAsync(cancellationToken))
        .Select(view => view.ToContract())
        .ToList();

    public Task<int> CountOwnedAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken) =>
        db.Views.CountAsync(view => view.SpaceId == spaceId && view.OwnerId == userId, cancellationToken);

    public async Task SaveAsync(View view, CancellationToken cancellationToken)
    {
        var fresh = view.ToEntity();
        if (await TrackedAsync(view.SpaceId, view.Id, cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Views.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(spaceId, id, cancellationToken) is { } tracked)
        {
            db.Views.Remove(tracked);
        }
    }

    /// <summary>The tracked row, including one added earlier in this request.</summary>
    private async Task<ViewEntity?> TrackedAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        db.Views.Local.FirstOrDefault(view => view.Id == id && view.SpaceId == spaceId)
        ?? await db.Views.FirstOrDefaultAsync(view => view.Id == id && view.SpaceId == spaceId, cancellationToken);
}
