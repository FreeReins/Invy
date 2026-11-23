using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="ITypeAccess"/>
internal sealed class TypeAccess(InvyDbContext db) : ITypeAccess
{
    public async Task<List<ItemType>> ListAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        // Loading tracks them, so types added or changed earlier in this request are seen as they are now.
        await db.ItemTypes.Where(type => type.SpaceId == spaceId).LoadAsync(cancellationToken);
        return db.ItemTypes.Local
            .Where(type => type.SpaceId == spaceId)
            .Select(type => type.ToContract())
            .OrderBy(type => type.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<ItemType?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(spaceId, id, cancellationToken))?.ToContract();

    public async Task SaveAsync(ItemType type, CancellationToken cancellationToken)
    {
        var fresh = type.ToEntity();
        if (await TrackedAsync(type.SpaceId, type.Id, cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.ItemTypes.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(spaceId, id, cancellationToken) is { } tracked)
        {
            db.ItemTypes.Remove(tracked);
        }
    }

    /// <summary>The tracked row, including one added earlier in this request. Always filtered by space.</summary>
    private async Task<ItemTypeEntity?> TrackedAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        db.ItemTypes.Local.FirstOrDefault(type => type.SpaceId == spaceId && type.Id == id)
        ?? await db.ItemTypes.FirstOrDefaultAsync(type => type.SpaceId == spaceId && type.Id == id, cancellationToken);
}
