using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Views kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed class FakeViews : IViewAccess
{
    public List<View> Rows { get; } = [];

    public Task<View?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(view => view.Id == id && view.SpaceId == spaceId) is { } view ? Copy(view) : null);

    public Task<List<View>> ListAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(view => view.SpaceId == spaceId && (view.Shared || view.OwnerId == userId))
            .OrderByDescending(view => view.Pinned).ThenBy(view => view.Name, StringComparer.Ordinal).ThenBy(view => view.Id)
            .Select(Copy).ToList());

    public Task<int> CountOwnedAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Count(view => view.SpaceId == spaceId && view.OwnerId == userId));

    public Task SaveAsync(View view, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == view.Id);
        Rows.Add(Copy(view));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == id && row.SpaceId == spaceId);
        return Task.CompletedTask;
    }

    private static View Copy(View view) => new()
    {
        Id = view.Id,
        SpaceId = view.SpaceId,
        OwnerId = view.OwnerId,
        Name = view.Name,
        Query = view.Query,
        Display = view.Display,
        Columns = [.. view.Columns],
        Sort = view.Sort,
        GroupBy = view.GroupBy,
        Shared = view.Shared,
        Pinned = view.Pinned,
        CreatedAt = view.CreatedAt,
        UpdatedAt = view.UpdatedAt,
    };
}
