using System.Globalization;
using System.Text.Json;
using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Managers;

/// <content>
/// The space features' item operations: low stock, lending, reminder dates and moving. A feature that is off is
/// hidden in the web page, not blocked here, so these work whatever the switches say.
/// </content>
internal sealed partial class ItemManager
{
    /// <summary>The most items a feature list returns.</summary>
    public const int MaxListed = 200;

    /// <summary>The longest a loan can be from today, in days.</summary>
    public const int MaxLoanDays = 3660;

    public async Task<ItemListResponse> ListLowStockAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return List(await items.ListLowAsync(spaceId, MaxListed + 1, cancellationToken));
    }

    public async Task<ItemResponse> SetLowStockAsync(Guid userId, Guid spaceId, Guid id, LowStockRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var update = new ItemRequest(
            item.Name, item.Marking, item.TypeId, null, item.HoldsItems, item.Quantity, item.Unit, item.Fields, item.Tags, item.Notes,
            request.Version ?? item.Version, request.At);
        return await UpdateItemAsync(userId, spaceId, id, update, cancellationToken);
    }

    public async Task<ItemListResponse> ListLoansAsync(Guid userId, Guid spaceId, bool overdue, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var dueBy = overdue ? clock.Today().AddDays(-1) : (DateOnly?)null;
        return List(await items.ListLentAsync(spaceId, dueBy, MaxListed + 1, cancellationToken));
    }

    public async Task<ItemResponse> LendAsync(Guid userId, Guid spaceId, Guid id, LendRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var to = ValidationEngine.Borrower(request.To);
        var today = clock.Today();
        if (request.DueOn is DateOnly due && (due < today || due > today.AddDays(MaxLoanDays)))
        {
            throw new RuleViolationException("A loan is due back between today and ten years from now. Pick another date, or leave it empty.");
        }

        switch (item.Status)
        {
            case ItemStatus.Lent:
                throw new RuleViolationException($"{item.Name} is already lent to {item.LentTo}. Mark it returned first.");
            case not ItemStatus.Active:
                throw new RuleViolationException($"{item.Name} is {item.Status.ToString().ToLowerInvariant()}, so it can't be lent. Restore or find it first.");
        }

        var now = clock.GetUtcNow();
        item.Status = ItemStatus.Lent;
        item.LentTo = to;
        item.LentAt = now;
        item.DueOn = request.DueOn;
        var lent = Event(item, userId, ItemEventKind.Lent, now);
        lent.Changes = [new ItemChange("Lent to", null, to), .. request.DueOn is DateOnly day ? [new ItemChange("Due back", null, Day(day))] : Array.Empty<ItemChange>()];
        return await SaveFeatureChangeAsync(item, lent, cancellationToken);
    }

    public async Task<ItemResponse> ReturnAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        if (item.LentAt is null)
        {
            return item.ToResponse();
        }

        var now = clock.GetUtcNow();
        var returned = Event(item, userId, ItemEventKind.Returned, now);
        returned.Changes = [new ItemChange("Lent to", item.LentTo, null)];
        if (item.Status == ItemStatus.Lent)
        {
            item.Status = ItemStatus.Active;
        }

        item.LentTo = null;
        item.LentAt = null;
        item.DueOn = null;
        return await SaveFeatureChangeAsync(item, returned, cancellationToken);
    }

    public async Task<RemindersResponse> ListRemindersAsync(Guid userId, Guid spaceId, int? days, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var ahead = Math.Clamp(days ?? 30, 0, ValidationEngine.MaxRemindDays);
        var fields = (await types.ListAsync(spaceId, cancellationToken)).SelectMany(type => type.Fields)
            .Where(field => field.Remind is not null).GroupBy(field => field.Id).ToDictionary(group => group.Key, group => group.First());
        var today = clock.Today();

        // Overdue upkeep stays on the list until it is done; a passed warranty doesn't.
        var found = await items.ListDatesAsync(spaceId, fields.Keys, today.AddDays(-365), today.AddDays(ahead), 1000, cancellationToken);
        var dates = found.Where(date => date.On >= today || fields[date.FieldId].RepeatMonths is not null)
            .Select(date => new ReminderDate(
                date.Item.ToSummary(), date.FieldId, fields[date.FieldId].Label, date.On, fields[date.FieldId].RepeatMonths is not null,
                date.On.DayNumber - today.DayNumber))
            .ToList();
        return new RemindersResponse(dates.Take(MaxListed).ToList(), dates.Count > MaxListed);
    }

    public async Task<ItemResponse> MarkUpkeepDoneAsync(Guid userId, Guid spaceId, Guid id, UpkeepDoneRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var before = await validation.ItemAsync(spaceId, id, cancellationToken);
        var type = await validation.ItemTypeAsync(spaceId, before.TypeId, cancellationToken);
        var field = type?.Fields.FirstOrDefault(field => field.Id == request.FieldId);
        if (field?.RepeatMonths is not int months)
        {
            throw new RuleViolationException($"That isn't an upkeep date of {before.Name}. Only a date field that repeats can be marked done.");
        }

        // The next upkeep is counted from today, when it was actually done.
        var item = before.Copy();
        item.Fields[field.Id] = JsonSerializer.SerializeToElement(
            clock.Today().AddMonths(months).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), QueryJson.Default.String);
        var now = clock.GetUtcNow();
        var changed = Event(item, userId, ItemEventKind.Changed, now);
        changed.Changes = itemEngine.Changes(before, item, type, type).ToList();
        item.Value = itemEngine.Value(item, type);
        item.IsExample = false;
        item.Version++;
        item.UpdatedAt = now;
        await items.SaveAsync(item, itemEngine.SortableValues(item, type), cancellationToken);
        await items.AddEventAsync(changed, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }

    public async Task<ItemListResponse> ListGoingAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return List(await items.ListGoingAsync(spaceId, MaxListed + 1, cancellationToken));
    }

    public async Task<ItemResponse> SetDestinationAsync(
        Guid userId, Guid spaceId, Guid id, DestinationRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var destination = await validation.ContainerAsync(spaceId, request.DestinationId, cancellationToken);
        ValidationEngine.MoveTarget(item, destination);
        if (item.DestinationId == destination?.Id)
        {
            return item.ToResponse();
        }

        var old = item.DestinationId is Guid oldId ? await items.FindAsync(spaceId, oldId, cancellationToken) : null;
        var now = clock.GetUtcNow();
        item.DestinationId = destination?.Id;
        var changed = Event(item, userId, ItemEventKind.Changed, now);
        changed.Changes = [new ItemChange("Going to", old?.Name, destination?.Name)];
        return await SaveFeatureChangeAsync(item, changed, cancellationToken);
    }

    public async Task<ItemResponse> MarkArrivedAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        if (item.DestinationId is not Guid destinationId)
        {
            throw new RuleViolationException($"{item.Name} has no destination. Say where it's going first, or move it yourself.");
        }

        var destination = await items.FindAsync(spaceId, destinationId, cancellationToken);
        item.DestinationId = null;
        if (destination is null || !destination.HoldsItems || destination.Status is ItemStatus.Disposed or ItemStatus.Archived)
        {
            throw new RuleViolationException($"{item.Name}'s destination no longer takes things. Pick another destination.");
        }

        if (item.ParentId == destination.Id)
        {
            // Already there: only the destination is cleared.
            var changed = Event(item, userId, ItemEventKind.Changed, clock.GetUtcNow());
            changed.Changes = [new ItemChange("Going to", destination.Name, null)];
            return await SaveFeatureChangeAsync(item, changed, cancellationToken);
        }

        // One Moved event; clearing the destination is saved with it.
        var moved = await MoveAsync(item, destination, userId, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return moved.ToResponse();
    }

    public async Task<ManifestResponse> GetManifestAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var box = await validation.ItemAsync(spaceId, id, cancellationToken);
        var destination = box.DestinationId is Guid destinationId ? await items.FindAsync(spaceId, destinationId, cancellationToken) : null;
        var path = destination is null
            ? []
            : (await items.FindManyAsync(spaceId, ItemPaths.Ids(destination.Path), cancellationToken))
                .OrderBy(place => place.Depth).Select(place => place.ToRef()).ToList();
        var contents = await items.ListChildrenAsync(spaceId, box.Id, null, MaxListed + 1, cancellationToken);
        return new ManifestResponse(
            box.ToSummary(), destination?.ToRef(), path, contents.Take(MaxListed).Select(item => item.ToSummary()).ToList(), contents.Count > MaxListed);
    }

    private static ItemListResponse List(IReadOnlyList<Item> found) =>
        new(found.Take(MaxListed).Select(item => item.ToSummary()).ToList(), found.Count > MaxListed);

    private static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Saves a change that leaves fields and amount alone, with its one event, and commits.</summary>
    private async Task<ItemResponse> SaveFeatureChangeAsync(Item item, ItemEvent itemEvent, CancellationToken cancellationToken)
    {
        item.IsExample = false;
        item.Version++;
        item.UpdatedAt = itemEvent.At;
        await items.SaveAsync(item, await ValuesAsync(item, cancellationToken), cancellationToken);
        await items.AddEventAsync(itemEvent, cancellationToken);
        await RecountLaterAsync(item.SpaceId, item.Path);
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }
}
