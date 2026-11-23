using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Timelines: an item's own, where it has been, and the space's activity.</content>
internal sealed partial class ItemManager
{
    /// <summary>How many events an item's timeline shows.</summary>
    public const int HistorySize = 200;

    /// <summary>How many events a page of activity holds.</summary>
    public const int ActivityPageSize = 50;

    public async Task<ItemHistoryResponse> GetHistoryAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        await validation.ItemAsync(spaceId, id, cancellationToken);
        var events = await items.ListEventsAsync(spaceId, id, HistorySize, cancellationToken);

        // Every container the item was ever in, at any depth: their moves moved it too.
        var moves = new List<ItemEvent>();
        var seen = new HashSet<Guid>();
        IReadOnlyCollection<Guid> next = [id];
        while (next.Count > 0 && seen.Count < 1_000)
        {
            seen.UnionWith(next);
            var found = await items.ListMovesAsync(spaceId, next, cancellationToken);
            moves.AddRange(found);
            next = timeline.ContainersIn(found).Where(container => !seen.Contains(container)).ToList();
        }

        var places = timeline.Places(id, moves);
        var refs = await RefsAsync(spaceId, events.SelectMany(Mentioned).Concat(places.SelectMany(stay => stay.Chain)), cancellationToken);
        var names = await ActorsAsync(cancellationToken);
        return new ItemHistoryResponse(
            events.Select(e => e.ToResponse(refs, names)).ToList(),
            places.Reverse().Select(stay => new ItemStay(stay.From, stay.Until, stay.Chain.Select(container => refs[container]).ToList())).ToList());
    }

    public async Task<ActivityResponse> ListActivityAsync(Guid userId, Guid spaceId, string? before, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var found = await items.ListActivityAsync(spaceId, ValidationEngine.EventCursor(before), ActivityPageSize + 1, cancellationToken);
        var page = found.Take(ActivityPageSize).ToList();
        var refs = await RefsAsync(spaceId, page.SelectMany(Mentioned), cancellationToken);
        var names = await ActorsAsync(cancellationToken);
        var next = found.Count > ActivityPageSize ? Mapping.Cursor(page[^1]) : null;
        return new ActivityResponse(page.Select(e => e.ToResponse(refs, names)).ToList(), next);
    }

    private static IEnumerable<Guid> Mentioned(ItemEvent itemEvent) =>
        new[] { itemEvent.ItemId, itemEvent.FromParent, itemEvent.ToParent }.OfType<Guid>();

    /// <summary>Each item named, as it is called now. One gone from the space is named as removed.</summary>
    private async Task<IReadOnlyDictionary<Guid, ItemRef>> RefsAsync(Guid spaceId, IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var wanted = ids.Distinct().ToList();
        var found = (await items.FindManyAsync(spaceId, wanted, cancellationToken)).ToDictionary(item => item.Id, item => item.ToRef());
        return wanted.ToDictionary(id => id, id => found.GetValueOrDefault(id) ?? new ItemRef(id, "A removed item", null));
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ActorsAsync(CancellationToken cancellationToken) =>
        (await accounts.ListUsersAsync(cancellationToken)).ToDictionary(user => user.Id, user => user.DisplayName);
}
