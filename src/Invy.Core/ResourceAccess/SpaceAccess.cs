using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="ISpaceAccess"/>
internal sealed class SpaceAccess(InvyDbContext db) : ISpaceAccess
{
    public async Task<Space?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(id, cancellationToken))?.ToContract();

    public async Task<List<Space>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        (await db.Spaces.AsNoTracking()
            .Include(space => space.Members)
            .Where(space => space.Members.Any(member => member.UserId == userId))
            .OrderBy(space => space.CreatedAt).ThenBy(space => space.Id)
            .ToListAsync(cancellationToken))
        .Select(space => space.ToContract())
        .ToList();

    public async Task<List<Space>> ListWithFeatureAsync(string feature, CancellationToken cancellationToken) =>
        (await db.Spaces.AsNoTracking()
            .Where(space => space.Features.Contains(feature))
            .OrderBy(space => space.Name).ThenBy(space => space.Id)
            .ToListAsync(cancellationToken))
        .Select(space => space.ToContract())
        .ToList();

    public async Task SaveAsync(Space space, CancellationToken cancellationToken)
    {
        var fresh = space.ToEntity();
        if (await TrackedAsync(space.Id, cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
            db.SyncChildren(tracked.Members, fresh.Members, member => member.UserId);
        }
        else
        {
            db.Spaces.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(id, cancellationToken) is { } tracked)
        {
            db.Spaces.Remove(tracked);
        }
    }

    /// <summary>The tracked row, including one added earlier in this request.</summary>
    private async Task<SpaceEntity?> TrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Spaces.Local.FirstOrDefault(space => space.Id == id)
        ?? await db.Spaces.Include(space => space.Members).FirstOrDefaultAsync(space => space.Id == id, cancellationToken);
}
