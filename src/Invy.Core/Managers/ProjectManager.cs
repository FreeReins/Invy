using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="IProjectManager"/>
internal sealed class ProjectManager(
    IProjectAccess projects,
    IItemAccess items,
    IMessageAccess messages,
    ValidationEngine validation,
    ItemEngine itemEngine,
    TimeProvider clock) : IProjectManager
{
    public async Task<IReadOnlyList<ProjectResponse>> ListProjectsAsync(Guid userId, Guid spaceId, bool includeDone, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return await ResponsesAsync(spaceId, await projects.ListAsync(spaceId, includeDone, cancellationToken), cancellationToken);
    }

    public async Task<ProjectResponse> GetProjectAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return await ResponseAsync(await validation.ProjectAsync(spaceId, id, cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectResponse>> ListForItemAsync(Guid userId, Guid spaceId, Guid itemId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return await ResponsesAsync(spaceId, await projects.ListForItemAsync(spaceId, itemId, cancellationToken), cancellationToken);
    }

    public async Task<ProjectResponse> CreateProjectAsync(Guid userId, Guid spaceId, ProjectRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var now = clock.GetUtcNow();
        var project = new Project { SpaceId = spaceId, Status = ProjectStatus.Open, CreatedAt = now, UpdatedAt = now, Version = 1 };
        ValidationEngine.ProjectDetails(project, request);
        await projects.SaveAsync(project, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ResponseAsync(project, cancellationToken);
    }

    public async Task<ProjectResponse> UpdateProjectAsync(Guid userId, Guid spaceId, Guid id, ProjectRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var project = await validation.ProjectAsync(spaceId, id, cancellationToken);
        if (request.Version != project.Version)
        {
            throw new ConflictException($"Someone else changed {project.Name} while you were editing it. Reload to see their change, then try again.");
        }

        ValidationEngine.ProjectDetails(project, request);
        return await SaveAsync(project, cancellationToken);
    }

    public async Task<ProjectResponse> ReserveAsync(Guid userId, Guid spaceId, Guid id, ReserveRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var project = await validation.ProjectAsync(spaceId, id, cancellationToken);
        var item = await validation.ItemAsync(spaceId, request.ItemId, cancellationToken);
        var entry = project.Items.FirstOrDefault(reserved => reserved.ItemId == item.Id);
        var current = entry?.Amount ?? 0;
        var amount = ValidationEngine.Reservation(project, item, request.Amount, current);
        if (amount == current)
        {
            return await ResponseAsync(project, cancellationToken);
        }

        if (amount == 0)
        {
            project.Items.Remove(entry!);
        }
        else if (entry is null)
        {
            project.Items.Add(new ProjectItem { ItemId = item.Id, Amount = amount });
        }
        else
        {
            entry.Amount = amount;
        }

        var after = item.Copy();
        after.Reserved = Math.Max(0, item.Reserved + amount - current);
        await items.ReserveAsync(spaceId, new Dictionary<Guid, decimal> { [item.Id] = amount - current }, cancellationToken);
        await AlertIfRanLowAsync(item, after);
        return await SaveAsync(project, cancellationToken);
    }

    public async Task<ProjectResponse> UseUpAsync(Guid userId, Guid spaceId, Guid id, UseUpRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var project = await validation.ProjectAsync(spaceId, id, cancellationToken);
        var item = await validation.ItemAsync(spaceId, request.ItemId, cancellationToken);
        var entry = project.Items.FirstOrDefault(reserved => reserved.ItemId == item.Id)
            ?? throw new RuleViolationException($"{project.Name} hasn't set aside any {item.Name}. Set some aside first.");
        var amount = request.Amount ?? entry.Amount;
        if (amount <= 0 || amount > entry.Amount)
        {
            throw new RuleViolationException($"Use up more than 0 and at most what {project.Name} set aside ({entry.Amount:0.###}).");
        }

        var before = item.Copy();
        var now = clock.GetUtcNow();
        ItemEvent itemEvent;
        if (item.Quantity is decimal quantity)
        {
            if (amount > quantity)
            {
                throw new RuleViolationException($"There {(quantity == 1 ? "is" : "are")} only {Mapping.Amount(item)} of {item.Name} left. Change its amount first.");
            }

            item.Quantity = quantity - amount;
            itemEvent = ItemEngine.Event(item, userId, ItemEventKind.QuantityChanged, now);
            itemEvent.QtyDelta = -amount;
        }
        else
        {
            item.Status = ItemStatus.Disposed;
            itemEvent = ItemEngine.Event(item, userId, ItemEventKind.Disposed, now);
        }

        item.Reserved = Math.Max(0, item.Reserved - amount);
        item.IsExample = false;
        item.Version++;
        item.UpdatedAt = now;
        var type = await validation.ItemTypeAsync(spaceId, item.TypeId, cancellationToken);
        item.Value = itemEngine.Value(item, type);
        await items.SaveAsync(item, itemEngine.SortableValues(item, type), cancellationToken);
        await items.AddEventAsync(itemEvent, cancellationToken);

        entry.Amount -= amount;
        if (entry.Amount == 0)
        {
            project.Items.Remove(entry);
        }

        if (ItemPaths.AncestorIds(item.Path) is { Count: > 0 } containers)
        {
            await messages.PublishAsync(new ItemsChanged(spaceId, containers));
        }

        await AlertIfRanLowAsync(before, item);
        return await SaveAsync(project, cancellationToken);
    }

    public async Task<ProjectResponse> FinishProjectAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var project = await validation.ProjectAsync(spaceId, id, cancellationToken);
        if (project.Status == ProjectStatus.Done)
        {
            return await ResponseAsync(project, cancellationToken);
        }

        await ReleaseAsync(project, cancellationToken);
        project.Status = ProjectStatus.Done;
        return await SaveAsync(project, cancellationToken);
    }

    public async Task DeleteProjectAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var project = await validation.ProjectAsync(spaceId, id, cancellationToken);
        await ReleaseAsync(project, cancellationToken);
        await projects.RemoveAsync(spaceId, id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    /// <summary>Gives back everything the project sets aside, in the caller's commit.</summary>
    private async Task ReleaseAsync(Project project, CancellationToken cancellationToken)
    {
        await items.ReserveAsync(project.SpaceId, project.Items.ToDictionary(entry => entry.ItemId, entry => -entry.Amount), cancellationToken);
        project.Items.Clear();
    }

    /// <summary>Queues the low-stock alert when the change took the item from above its level to at or below it.</summary>
    private async Task AlertIfRanLowAsync(Item before, Item after)
    {
        if (!before.IsLow && after.IsLow && after.Status == ItemStatus.Active)
        {
            await messages.PublishAsync(new StockRanLow(after.SpaceId, after.Id));
        }
    }

    private async Task<ProjectResponse> SaveAsync(Project project, CancellationToken cancellationToken)
    {
        project.Version++;
        project.UpdatedAt = clock.GetUtcNow();
        await projects.SaveAsync(project, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ResponseAsync(project, cancellationToken);
    }

    private async Task<ProjectResponse> ResponseAsync(Project project, CancellationToken cancellationToken) =>
        (await ResponsesAsync(project.SpaceId, [project], cancellationToken))[0];

    /// <summary>The projects with their items as they are now, read in one go.</summary>
    private async Task<IReadOnlyList<ProjectResponse>> ResponsesAsync(Guid spaceId, IReadOnlyList<Project> found, CancellationToken cancellationToken)
    {
        var ids = found.SelectMany(project => project.Items.Select(entry => entry.ItemId)).Distinct().ToList();
        var byId = (await items.FindManyAsync(spaceId, ids, cancellationToken)).ToDictionary(item => item.Id);
        return found.Select(project => new ProjectResponse(
                project.Id, project.Name, project.Notes, project.Status, project.CreatedAt, project.UpdatedAt, project.Version,
                project.Items.Where(entry => byId.ContainsKey(entry.ItemId))
                    .Select(entry => new ProjectItemResponse(byId[entry.ItemId].ToSummary(), entry.Amount))
                    .OrderBy(entry => entry.Item.Name, StringComparer.CurrentCulture).ToList()))
            .ToList();
    }
}
