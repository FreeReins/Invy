using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

/// <content>The lists the space features read, in memory.</content>
internal sealed partial class FakeItems
{
    public Task<List<Item>> ListLowAsync(Guid spaceId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId).Where(item => item.IsLow)
            .OrderBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Id).Take(limit).Select(Copy).ToList());

    public Task<List<Item>> ListLentAsync(Guid spaceId, DateOnly? dueBy, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId).Where(item => item.LentAt is not null && (dueBy is null || item.DueOn <= dueBy))
            .OrderBy(item => item.DueOn is null).ThenBy(item => item.DueOn).ThenBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Id)
            .Take(limit).Select(Copy).ToList());

    public Task<List<Item>> ListGoingAsync(Guid spaceId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId).Where(item => item.DestinationId is not null)
            .OrderBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Id).Take(limit).Select(Copy).ToList());

    public Task<List<ItemDate>> ListDatesAsync(
        Guid spaceId, IReadOnlyCollection<Guid> fieldIds, DateOnly first, DateOnly last, int limit, CancellationToken cancellationToken)
    {
        var current = Current(spaceId).ToDictionary(item => item.Id);
        return Task.FromResult(Values
            .Where(value => value.SpaceId == spaceId && fieldIds.Contains(value.FieldId) && value.Date >= first && value.Date <= last && current.ContainsKey(value.ItemId))
            .Select(value => new ItemDate(Copy(current[value.ItemId]), value.FieldId, value.Date!.Value))
            .OrderBy(date => date.On).ThenBy(date => date.Item.Name, StringComparer.Ordinal).Take(limit).ToList());
    }

    public Task ReserveAsync(Guid spaceId, IReadOnlyDictionary<Guid, decimal> changes, CancellationToken cancellationToken)
    {
        foreach (var item in Rows.Where(item => item.SpaceId == spaceId && changes.ContainsKey(item.Id)))
        {
            item.Reserved = Math.Max(0, item.Reserved + changes[item.Id]);
        }

        return Task.CompletedTask;
    }

    /// <summary>How many times items were added in bulk.</summary>
    public int BulkInserts { get; private set; }

    public Task<Item?> FindContainerAsync(Guid spaceId, Guid? parentId, string name, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId).Where(item => item.ParentId == parentId && item.HoldsItems
                && string.Equals(item.Name, name, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(item => item.Id).Select(Copy).FirstOrDefault());

    public Task InsertManyAsync(IReadOnlyList<Item> items, IReadOnlyList<ItemEvent> events, IReadOnlyList<ItemValue> values, CancellationToken cancellationToken)
    {
        BulkInserts++;
        Rows.AddRange(items.Select(Copy));
        Events.AddRange(events);
        Values.AddRange(values);
        return Task.CompletedTask;
    }
}
