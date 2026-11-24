using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Items, events and sortable values kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed partial class FakeItems : IItemAccess
{
    public List<Item> Rows { get; } = [];

    public List<ItemEvent> Events { get; } = [];

    public List<ItemValue> Values { get; } = [];

    /// <summary>How many times a subtree was moved in one statement.</summary>
    public int SubtreeMoves { get; private set; }

    public Item Named(string name) => Rows.Single(item => item.Name == name);

    public Task<Item?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(item => item.SpaceId == spaceId && item.Id == id) is { } item ? Copy(item) : null);

    public Task<List<Item>> FindManyAsync(Guid spaceId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(item => item.SpaceId == spaceId && ids.Contains(item.Id)).Select(Copy).ToList());

    public Task<List<Item>> ListChildrenAsync(Guid spaceId, Guid? parentId, ItemCursor? after, int limit, CancellationToken cancellationToken) =>
        Page(Current(spaceId).Where(item => item.ParentId == parentId && (parentId is not null || item.TypeId is not null || item.HoldsItems)), after, limit);

    public Task<List<Item>> ListInboxAsync(Guid spaceId, ItemCursor? after, int limit, CancellationToken cancellationToken) =>
        Page(Current(spaceId).Where(item => item.ParentId is null && item.TypeId is null && !item.HoldsItems), after, limit);

    public Task<List<Item>> ListInboxToSuggestAsync(Guid spaceId, DateTimeOffset? askedBefore, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId)
            .Where(item => item.ParentId is null && item.TypeId is null && !item.HoldsItems && item.SuggestedTypeId is null)
            .Where(item => item.TypeSuggestedAt is null || item.TypeSuggestedAt < askedBefore)
            .OrderBy(item => item.Id).Take(limit).Select(Copy).ToList());

    public Task SuggestTypesAsync(Guid spaceId, IReadOnlyDictionary<Guid, Guid?> suggestions, DateTimeOffset at, CancellationToken cancellationToken)
    {
        foreach (var item in Rows.Where(item => item.SpaceId == spaceId && suggestions.ContainsKey(item.Id)))
        {
            item.SuggestedTypeId = suggestions[item.Id];
            item.TypeSuggestedAt = at;
        }

        return Task.CompletedTask;
    }

    public Task<Dictionary<Guid, int>> CountChildrenAsync(Guid spaceId, IReadOnlyCollection<Guid> parentIds, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId).Where(item => item.ParentId is Guid parent && parentIds.Contains(parent))
            .GroupBy(item => item.ParentId!.Value).ToDictionary(group => group.Key, group => group.Count()));

    public Task SaveAsync(Item item, IReadOnlyList<ItemValue> values, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == item.Id);
        Rows.Add(Copy(item));
        Values.RemoveAll(value => value.ItemId == item.Id);
        Values.AddRange(values);
        return Task.CompletedTask;
    }

    public Task MoveContentsAsync(Guid spaceId, string oldPath, string newPath, CancellationToken cancellationToken)
    {
        SubtreeMoves++;
        foreach (var item in Rows.Where(item => item.SpaceId == spaceId && item.Path.StartsWith(oldPath + ".", StringComparison.Ordinal)))
        {
            item.Path = newPath + item.Path[oldPath.Length..];
            item.Depth = ItemPaths.Depth(item.Path);
        }

        return Task.CompletedTask;
    }

    public Task MarkSeenAsync(Guid spaceId, IReadOnlyCollection<Guid> ids, Guid? contentsOf, DateTimeOffset at, CancellationToken cancellationToken)
    {
        foreach (var item in Rows.Where(item => item.SpaceId == spaceId && (ids.Contains(item.Id)
            || (contentsOf is not null && item.ParentId == contentsOf && item.Status is not (ItemStatus.Disposed or ItemStatus.Archived)))))
        {
            item.LastSeenAt = at;
        }

        return Task.CompletedTask;
    }

    public Task AddEventAsync(ItemEvent itemEvent, CancellationToken cancellationToken)
    {
        Events.Add(itemEvent);
        return Task.CompletedTask;
    }

    public Task<List<ItemEvent>> ListEventsAsync(Guid spaceId, Guid itemId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Events.Where(e => e.SpaceId == spaceId && (e.ItemId == itemId || e.FromParent == itemId || e.ToParent == itemId))
            .OrderByDescending(e => e.At).ThenByDescending(e => e.Id).Take(limit).ToList());

    public Task<List<ItemEvent>> ListMovesAsync(Guid spaceId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken) =>
        Task.FromResult(Events.Where(e => e.SpaceId == spaceId && itemIds.Contains(e.ItemId) && e.Kind is ItemEventKind.Created or ItemEventKind.Moved)
            .OrderBy(e => e.At).ThenBy(e => e.Id).ToList());

    public Task<List<ItemEvent>> ListActivityAsync(Guid spaceId, EventCursor? before, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Events.Where(e => e.SpaceId == spaceId && (before is null || e.At < before.At || (e.At == before.At && e.Id.CompareTo(before.Id) < 0)))
            .OrderByDescending(e => e.At).ThenByDescending(e => e.Id).Take(limit).ToList());

    public Task<List<ItemEvent>> ListMatchingEventsAsync(
        Guid spaceId, ItemQuery query, EventCursor? before, int limit, CancellationToken cancellationToken)
    {
        var ids = Matching(spaceId, query).Select(item => item.Id).ToHashSet();
        return Task.FromResult(Events.Where(e => e.SpaceId == spaceId && ids.Contains(e.ItemId)
                && (before is null || e.At < before.At || (e.At == before.At && e.Id.CompareTo(before.Id) < 0)))
            .OrderByDescending(e => e.At).ThenByDescending(e => e.Id).Take(limit).ToList());
    }

    public Task<List<PlaceTotal>> TotalsByPlaceAsync(Guid spaceId, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId).Where(item => item.ParentId == null)
            .GroupBy(item => item.HoldsItems ? (Guid?)item.Id : null)
            .Select(g => new PlaceTotal(g.Key, g.Sum(i => i.HoldsItems ? i.ContentCount : 1), g.Sum(i => (i.HoldsItems ? i.ContentValue : 0) + (i.Value ?? 0))))
            .ToList());

    public Task<List<TypeTotal>> TotalsByTypeAsync(Guid spaceId, CancellationToken cancellationToken) =>
        Task.FromResult(Current(spaceId).GroupBy(item => item.TypeId)
            .Select(g => new TypeTotal(g.Key, g.Count(), g.Count(i => i.Value != null), g.Sum(i => i.Value ?? 0)))
            .ToList());

    private IEnumerable<Item> Current(Guid spaceId) =>
        Rows.Where(item => item.SpaceId == spaceId && item.Status is not (ItemStatus.Disposed or ItemStatus.Archived));

    private static Task<List<Item>> Page(IEnumerable<Item> rows, ItemCursor? after, int limit) =>
        Task.FromResult(rows
            .Where(item => after is null || string.CompareOrdinal(item.Name, after.Name) > 0 || (item.Name == after.Name && item.Id.CompareTo(after.Id) > 0))
            .OrderBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Id).Take(limit).Select(Copy).ToList());

    private static Item Copy(Item item) => new()
    {
        Id = item.Id,
        SpaceId = item.SpaceId,
        TypeId = item.TypeId,
        Name = item.Name,
        Marking = item.Marking,
        LabelCode = item.LabelCode,
        ParentId = item.ParentId,
        Path = item.Path,
        Depth = item.Depth,
        HoldsItems = item.HoldsItems,
        Quantity = item.Quantity,
        Unit = item.Unit,
        Fields = item.Fields.ToDictionary(pair => pair.Key, pair => pair.Value.Clone()),
        Tags = [.. item.Tags],
        Notes = item.Notes,
        Status = item.Status,
        LastSeenAt = item.LastSeenAt,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        Value = item.Value,
        ContentCount = item.ContentCount,
        ContentValue = item.ContentValue,
        SuggestedTypeId = item.SuggestedTypeId,
        TypeSuggestedAt = item.TypeSuggestedAt,
        IsExample = item.IsExample,
        Version = item.Version,
    };
}
