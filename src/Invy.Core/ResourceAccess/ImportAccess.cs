using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="IImportAccess"/>
internal sealed class ImportAccess(InvyDbContext db) : IImportAccess
{
    public async Task<Import?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(spaceId, id, cancellationToken))?.ToContract();

    public async Task<List<Import>> ListAsync(Guid spaceId, int limit, CancellationToken cancellationToken) =>
        (await db.Imports.AsNoTracking()
            .Where(import => import.SpaceId == spaceId)
            .OrderByDescending(import => import.CreatedAt).ThenByDescending(import => import.Id)
            .Take(limit)
            .ToListAsync(cancellationToken))
        .Select(import => import.ToContract())
        .ToList();

    public Task<bool> FileInUseAsync(string path, Guid exceptId, CancellationToken cancellationToken) =>
        db.Imports.AnyAsync(import => import.FilePath == path && import.Id != exceptId, cancellationToken);

    public async Task SaveAsync(Import import, CancellationToken cancellationToken)
    {
        var fresh = import.ToEntity();
        if (await TrackedAsync(import.SpaceId, import.Id, cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Imports.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(spaceId, id, cancellationToken) is { } tracked)
        {
            db.Imports.Remove(tracked);
        }
    }

    private async Task<ImportEntity?> TrackedAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        db.Imports.Local.FirstOrDefault(import => import.SpaceId == spaceId && import.Id == id)
        ?? await db.Imports.FirstOrDefaultAsync(import => import.SpaceId == spaceId && import.Id == id, cancellationToken);
}
