using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <content>The lists the space features read: low stock, loans, reminder dates and reservations.</content>
internal sealed partial class ItemAccess
{
    public async Task<List<Item>> ListLowAsync(Guid spaceId, int limit, CancellationToken cancellationToken) =>
        (await Current(spaceId)
            .Where(item => item.LowStockAt != null && item.Quantity != null && item.Quantity - item.Reserved <= item.LowStockAt)
            .OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Take(limit)
            .ToListAsync(cancellationToken))
        .Select(item => item.ToContract())
        .ToList();

    public async Task<List<Item>> ListLentAsync(Guid spaceId, DateOnly? dueBy, int limit, CancellationToken cancellationToken)
    {
        var rows = Current(spaceId).Where(item => item.LentAt != null);
        if (dueBy is not null)
        {
            rows = rows.Where(item => item.DueOn != null && item.DueOn <= dueBy);
        }

        return (await rows.OrderBy(item => item.DueOn == null).ThenBy(item => item.DueOn).ThenBy(item => item.Name).ThenBy(item => item.Id)
                .Take(limit)
                .ToListAsync(cancellationToken))
            .Select(item => item.ToContract())
            .ToList();
    }

    public async Task<List<Item>> ListGoingAsync(Guid spaceId, int limit, CancellationToken cancellationToken) =>
        (await Current(spaceId)
            .Where(item => item.DestinationId != null)
            .OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Take(limit)
            .ToListAsync(cancellationToken))
        .Select(item => item.ToContract())
        .ToList();

    public async Task<List<ItemDate>> ListDatesAsync(
        Guid spaceId, IReadOnlyCollection<Guid> fieldIds, DateOnly first, DateOnly last, int limit, CancellationToken cancellationToken)
    {
        if (fieldIds.Count == 0)
        {
            return [];
        }

        var rows = await (
                from value in db.ItemValues.AsNoTracking()
                join item in Current(spaceId) on value.ItemId equals item.Id
                where value.SpaceId == spaceId && fieldIds.Contains(value.FieldId) && value.Date >= first && value.Date <= last
                orderby value.Date, item.Name, item.Id
                select new { item, value.FieldId, value.Date })
            .Take(limit)
            .ToListAsync(cancellationToken);
        return rows.Select(row => new ItemDate(row.item.ToContract(), row.FieldId, row.Date!.Value)).ToList();
    }

    public async Task ReserveAsync(Guid spaceId, IReadOnlyDictionary<Guid, decimal> changes, CancellationToken cancellationToken)
    {
        if (changes.Count == 0)
        {
            return;
        }

        if (db.Database.CurrentTransaction is null)
        {
            await db.Database.BeginTransactionAsync(cancellationToken);
        }

        // One statement per distinct change; a project rarely touches more than a few amounts at once.
        foreach (var (id, delta) in changes)
        {
            await db.Items.Where(item => item.SpaceId == spaceId && item.Id == id)
                .ExecuteUpdateAsync(set => set.SetProperty(item => item.Reserved, item => item.Reserved + delta < 0 ? 0 : item.Reserved + delta), cancellationToken);
            if (db.Items.Local.FirstOrDefault(item => item.SpaceId == spaceId && item.Id == id) is { } tracked)
            {
                // Keep a tracked row in step, so a later save in this request doesn't put the old total back.
                var reserved = db.Entry(tracked).Property(item => item.Reserved);
                reserved.CurrentValue = Math.Max(0, reserved.CurrentValue + delta);
                reserved.OriginalValue = reserved.CurrentValue;
            }
        }
    }
}
