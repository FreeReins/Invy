using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <content>The timeline: events are only ever added.</content>
internal sealed partial class ItemAccess
{
    public Task AddEventAsync(ItemEvent itemEvent, CancellationToken cancellationToken)
    {
        db.ItemEvents.Add(itemEvent.ToEntity());
        return Task.CompletedTask;
    }

    public async Task<List<ItemEvent>> ListEventsAsync(Guid spaceId, Guid itemId, int limit, CancellationToken cancellationToken) =>
        (await db.ItemEvents.AsNoTracking()
            .Where(e => e.SpaceId == spaceId && (e.ItemId == itemId || e.FromParent == itemId || e.ToParent == itemId))
            .OrderByDescending(e => e.At).ThenByDescending(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken))
        .Select(e => e.ToContract())
        .ToList();

    public async Task<List<ItemEvent>> ListMovesAsync(Guid spaceId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken) =>
        itemIds.Count == 0
            ? []
            : (await db.ItemEvents.AsNoTracking()
                .Where(e => e.SpaceId == spaceId && itemIds.Contains(e.ItemId) && (e.Kind == ItemEventKind.Created || e.Kind == ItemEventKind.Moved))
                .OrderBy(e => e.At).ThenBy(e => e.Id)
                .ToListAsync(cancellationToken))
            .Select(e => e.ToContract())
            .ToList();

    public async Task<List<ItemEvent>> ListActivityAsync(Guid spaceId, EventCursor? before, int limit, CancellationToken cancellationToken)
    {
        IQueryable<ItemEventEntity> rows = db.ItemEvents.AsNoTracking().Where(e => e.SpaceId == spaceId);
        if (before is not null)
        {
            rows = rows.Where(e => e.At < before.At || (e.At == before.At && e.Id.CompareTo(before.Id) < 0));
        }

        return (await rows.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).Take(limit).ToListAsync(cancellationToken))
            .Select(e => e.ToContract())
            .ToList();
    }
}
