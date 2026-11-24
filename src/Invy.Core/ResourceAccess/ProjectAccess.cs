using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="IProjectAccess"/>
internal sealed class ProjectAccess(InvyDbContext db) : IProjectAccess
{
    public async Task<Project?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(spaceId, id, cancellationToken))?.ToContract();

    public async Task<List<Project>> ListAsync(Guid spaceId, bool includeDone, CancellationToken cancellationToken) =>
        (await db.Projects.AsNoTracking().Include(project => project.Items)
            .Where(project => project.SpaceId == spaceId && (includeDone || project.Status == ProjectStatus.Open))
            .OrderBy(project => project.Status).ThenBy(project => project.Name).ThenBy(project => project.Id)
            .ToListAsync(cancellationToken))
        .Select(project => project.ToContract())
        .ToList();

    public async Task<List<Project>> ListForItemAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken) =>
        (await db.Projects.AsNoTracking().Include(project => project.Items)
            .Where(project => project.SpaceId == spaceId && project.Status == ProjectStatus.Open && project.Items.Any(item => item.ItemId == itemId))
            .OrderBy(project => project.Name).ThenBy(project => project.Id)
            .ToListAsync(cancellationToken))
        .Select(project => project.ToContract())
        .ToList();

    public Task<bool> AnyAsync(Guid spaceId, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(project => project.SpaceId == spaceId, cancellationToken);

    public async Task SaveAsync(Project project, CancellationToken cancellationToken)
    {
        var fresh = project.ToEntity();
        if (await TrackedAsync(project.SpaceId, project.Id, cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
            db.SyncChildren(tracked.Items, fresh.Items, item => item.ItemId);
        }
        else
        {
            db.Projects.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(spaceId, id, cancellationToken) is { } tracked)
        {
            db.Projects.Remove(tracked);
        }
    }

    /// <summary>The tracked row, including one added earlier in this request. Always filtered by space.</summary>
    private async Task<ProjectEntity?> TrackedAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        db.Projects.Local.FirstOrDefault(project => project.SpaceId == spaceId && project.Id == id)
        ?? await db.Projects.Include(project => project.Items)
            .FirstOrDefaultAsync(project => project.SpaceId == spaceId && project.Id == id, cancellationToken);
}
