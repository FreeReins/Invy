using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <content>Value totals for reports: by place at the top from the containers' rollups, and by type.</content>
internal sealed partial class ItemAccess
{
    public async Task<List<PlaceTotal>> TotalsByPlaceAsync(Guid spaceId, CancellationToken cancellationToken) =>
        (await db.Items.AsNoTracking()
            .Where(i => i.SpaceId == spaceId && i.ParentId == null && i.Status != ItemStatus.Disposed && i.Status != ItemStatus.Archived)
            .GroupBy(i => i.HoldsItems ? (Guid?)i.Id : null)
            .Select(g => new
            {
                g.Key,
                Count = g.Sum(i => i.HoldsItems ? i.ContentCount : 1),
                Value = g.Sum(i => (i.HoldsItems ? i.ContentValue : 0) + (i.Value ?? 0)),
            })
            .ToListAsync(cancellationToken))
        .Select(row => new PlaceTotal(row.Key, row.Count, row.Value))
        .ToList();

    public async Task<List<TypeTotal>> TotalsByTypeAsync(Guid spaceId, CancellationToken cancellationToken) =>
        (await db.Items.AsNoTracking()
            .Where(i => i.SpaceId == spaceId && i.Status != ItemStatus.Disposed && i.Status != ItemStatus.Archived)
            .GroupBy(i => i.TypeId)
            .Select(g => new { g.Key, Count = g.Count(), Valued = g.Count(i => i.Value != null), Value = g.Sum(i => i.Value ?? 0) })
            .ToListAsync(cancellationToken))
        .Select(row => new TypeTotal(row.Key, row.Count, row.Valued, row.Value))
        .ToList();
}
