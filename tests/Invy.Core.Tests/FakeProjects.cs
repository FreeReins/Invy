using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Projects kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed class FakeProjects : IProjectAccess
{
    public List<Project> Rows { get; } = [];

    public Task<Project?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(project => project.SpaceId == spaceId && project.Id == id) is { } project ? Copy(project) : null);

    public Task<List<Project>> ListAsync(Guid spaceId, bool includeDone, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(project => project.SpaceId == spaceId && (includeDone || project.Status == ProjectStatus.Open))
            .OrderBy(project => project.Status).ThenBy(project => project.Name, StringComparer.Ordinal).Select(Copy).ToList());

    public Task<List<Project>> ListForItemAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(project => project.SpaceId == spaceId && project.Status == ProjectStatus.Open && project.Items.Any(item => item.ItemId == itemId))
            .OrderBy(project => project.Name, StringComparer.Ordinal).Select(Copy).ToList());

    public Task<bool> AnyAsync(Guid spaceId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Any(project => project.SpaceId == spaceId));

    public Task SaveAsync(Project project, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == project.Id);
        Rows.Add(Copy(project));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.SpaceId == spaceId && row.Id == id);
        return Task.CompletedTask;
    }

    /// <summary>True when an open project with this name sets aside some of the item, for <c>project:</c> searches.</summary>
    public bool Reserves(Guid itemId, string name) =>
        Rows.Any(project => project.Status == ProjectStatus.Open && string.Equals(project.Name, name, StringComparison.OrdinalIgnoreCase)
            && project.Items.Any(item => item.ItemId == itemId));

    private static Project Copy(Project project) => new()
    {
        Id = project.Id,
        SpaceId = project.SpaceId,
        Name = project.Name,
        Notes = project.Notes,
        Status = project.Status,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt,
        Version = project.Version,
        Items = project.Items.Select(item => new ProjectItem { ItemId = item.ItemId, Amount = item.Amount }).ToList(),
    };
}
