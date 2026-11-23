using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="ILabelAccess"/>
internal sealed class LabelAccess(InvyDbContext db) : ILabelAccess
{
    public async Task<Label?> FindAsync(string code, CancellationToken cancellationToken) =>
        (await TrackedAsync(code, cancellationToken))?.ToContract();

    public async Task<HashSet<string>> ExistingAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        var stored = await db.Labels.AsNoTracking().Where(label => codes.Contains(label.Code)).Select(label => label.Code).ToListAsync(cancellationToken);
        return [.. stored, .. db.Labels.Local.Select(label => label.Code).Where(codes.Contains)];
    }

    public Task<int> CountAsync(CancellationToken cancellationToken) => db.Labels.CountAsync(cancellationToken);

    public async Task<Label?> FindForItemAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken) =>
        (db.Labels.Local.FirstOrDefault(label => label.SpaceId == spaceId && label.ItemId == itemId)
            ?? await db.Labels.FirstOrDefaultAsync(label => label.SpaceId == spaceId && label.ItemId == itemId, cancellationToken))
        ?.ToContract();

    public async Task<List<Label>> ListAsync(Guid spaceId, bool unclaimed, string? after, int limit, CancellationToken cancellationToken)
    {
        var rows = db.Labels.AsNoTracking().Where(label => label.SpaceId == spaceId);
        if (unclaimed)
        {
            rows = rows.Where(label => label.ItemId == null);
        }

        if (after is not null)
        {
            rows = rows.Where(label => string.Compare(label.Code, after) > 0);
        }

        return (await rows.OrderBy(label => label.Code).Take(limit).ToListAsync(cancellationToken)).Select(label => label.ToContract()).ToList();
    }

    public async Task SaveAsync(Label label, CancellationToken cancellationToken)
    {
        var fresh = label.ToEntity();
        if (await TrackedAsync(label.Code, cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Labels.Add(fresh);
        }
    }

    /// <summary>The tracked row, including one added earlier in this request.</summary>
    private async Task<LabelEntity?> TrackedAsync(string code, CancellationToken cancellationToken) =>
        db.Labels.Local.FirstOrDefault(label => label.Code == code)
        ?? await db.Labels.FirstOrDefaultAsync(label => label.Code == code, cancellationToken);
}
