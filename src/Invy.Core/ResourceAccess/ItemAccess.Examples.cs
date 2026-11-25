using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <content>Example items from a starter pack, and what a space has so far.</content>
internal sealed partial class ItemAccess
{
    public async Task<SpaceProgress> ProgressAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var mine = Current(spaceId).Where(item => !item.IsExample);
        var found = await db.Spaces.AsNoTracking()
            .Where(space => space.Id == spaceId)
            .Select(space => new SpaceProgress
            {
                HasPlaces = mine.Any(item => item.ParentId == null && item.HoldsItems),
                HasPutAway = mine.Any(item => item.ParentId != null),
                HasValues = mine.Any(item => item.Value != null),
                HasSeen = mine.Any(item => item.LastSeenAt != null),
                HasLowStockLevels = mine.Any(item => item.LowStockAt != null),
                HasLent = db.ItemEvents.Any(e => e.SpaceId == spaceId && e.Kind == ItemEventKind.Lent),
                HasDestinations = mine.Any(item => item.DestinationId != null),
                ExamplesLeft = db.Items.Count(item => item.SpaceId == spaceId && item.IsExample),
            })
            .FirstOrDefaultAsync(cancellationToken);
        return found ?? new SpaceProgress();
    }

    /// <summary>Finds the examples with nothing of the person's own inside, then deletes them in one statement.</summary>
    public async Task<List<string>> RemoveExamplesAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            await db.Database.BeginTransactionAsync(cancellationToken);
        }

        var ids = await db.Database.SqlQuery<Guid>(
            $"""
            SELECT e."Id" AS "Value" FROM "Items" AS e
            WHERE e."SpaceId" = {spaceId} AND e."IsExample"
              AND NOT EXISTS (
                SELECT 1 FROM "ItemPaths" AS ep
                JOIN "ItemPaths" AS dp ON dp."Path" <@ ep."Path"
                JOIN "Items" AS d ON d."Id" = dp."Id"
                WHERE ep."Id" = e."Id" AND d."SpaceId" = {spaceId} AND NOT d."IsExample")
            """).ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return [];
        }

        var paths = await db.Items.AsNoTracking()
            .Where(item => item.SpaceId == spaceId && ids.Contains(item.Id))
            .Select(item => item.Place.Path)
            .ToListAsync(cancellationToken);

        // Events, sortable values and attachments go with their items through the foreign keys; labels are freed.
        await db.Items.Where(item => item.SpaceId == spaceId && ids.Contains(item.Id)).ExecuteDeleteAsync(cancellationToken);
        foreach (var tracked in db.Items.Local.Where(item => ids.Contains(item.Id)).ToList())
        {
            db.Entry(tracked).State = EntityState.Detached;
        }

        return paths;
    }
}
