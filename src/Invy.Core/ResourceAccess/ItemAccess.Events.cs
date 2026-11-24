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

    public Task<List<ItemEvent>> ListActivityAsync(Guid spaceId, EventCursor? before, int limit, CancellationToken cancellationToken) =>
        PageEventsAsync(db.ItemEvents.AsNoTracking().Where(e => e.SpaceId == spaceId), before, limit, cancellationToken);

    public Task<List<ItemEvent>> ListMatchingEventsAsync(
        Guid spaceId, ItemQuery query, EventCursor? before, int limit, CancellationToken cancellationToken)
    {
        // The matching items come from the same WHERE clause as search (SearchSql), so the query text never reaches SQL.
        // Every value in the WHERE clause is a parameter, so the text built here holds only SQL chosen by SearchSql.
        var sql = new SearchSql(spaceId);
        var text = $"SELECT * FROM \"ItemEvents\" WHERE \"ItemId\" IN (SELECT i.\"Id\" FROM \"Items\" i WHERE {sql.Where(query)})";
        var matching = db.ItemEvents
            .FromSqlRaw(text, sql.Parameters.ToArray<object>())
            .AsNoTracking()
            .Where(e => e.SpaceId == spaceId);
        return PageEventsAsync(matching, before, limit, cancellationToken);
    }

    private static async Task<List<ItemEvent>> PageEventsAsync(
        IQueryable<ItemEventEntity> rows, EventCursor? before, int limit, CancellationToken cancellationToken)
    {
        if (before is not null)
        {
            rows = rows.Where(e => e.At < before.At || (e.At == before.At && e.Id.CompareTo(before.Id) < 0));
        }

        return (await rows.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).Take(limit).ToListAsync(cancellationToken))
            .Select(e => e.ToContract())
            .ToList();
    }
}
