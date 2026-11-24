using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Imports kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed class FakeImports : IImportAccess
{
    public List<Import> Rows { get; } = [];

    public Task<Import?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(import => import.SpaceId == spaceId && import.Id == id) is { } import ? Copies.Of(import) : null);

    public Task<List<Import>> ListAsync(Guid spaceId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(import => import.SpaceId == spaceId).OrderByDescending(import => import.CreatedAt).ThenByDescending(import => import.Id)
            .Take(limit).Select(Copies.Of).ToList());

    public Task<bool> FileInUseAsync(string path, Guid exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Any(import => import.FilePath == path && import.Id != exceptId));

    public Task SaveAsync(Import import, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == import.Id);
        Rows.Add(Copies.Of(import));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.SpaceId == spaceId && row.Id == id);
        return Task.CompletedTask;
    }
}
