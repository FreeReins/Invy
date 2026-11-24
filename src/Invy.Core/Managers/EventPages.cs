using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <summary>A page of events with the items and people they name, shared by the activity feed and views.</summary>
internal static class EventPages
{
    /// <summary>How many events a page holds.</summary>
    public const int PageSize = 50;

    /// <summary>
    /// The first <see cref="PageSize"/> of <paramref name="found"/> (read with one extra, to know whether more follow),
    /// each item named as it is called now and each person by display name.
    /// </summary>
    public static async Task<ActivityResponse> ToPageAsync(
        IItemAccess items, IAccountAccess accounts, Guid spaceId, List<ItemEvent> found, CancellationToken cancellationToken)
    {
        var page = found.Take(PageSize).ToList();
        var refs = await RefsAsync(items, spaceId, page.SelectMany(Mentioned), cancellationToken);
        var names = await ActorsAsync(accounts, cancellationToken);
        var next = found.Count > PageSize ? Mapping.Cursor(page[^1]) : null;
        return new ActivityResponse(page.Select(e => e.ToResponse(refs, names)).ToList(), next);
    }

    /// <summary>The items an event names: itself and where it went from and to.</summary>
    public static IEnumerable<Guid> Mentioned(ItemEvent itemEvent) =>
        new[] { itemEvent.ItemId, itemEvent.FromParent, itemEvent.ToParent }.OfType<Guid>();

    /// <summary>Each item named, as it is called now. One gone from the space is named as removed.</summary>
    public static async Task<IReadOnlyDictionary<Guid, ItemRef>> RefsAsync(
        IItemAccess items, Guid spaceId, IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var wanted = ids.Distinct().ToList();
        var found = (await items.FindManyAsync(spaceId, wanted, cancellationToken)).ToDictionary(item => item.Id, item => item.ToRef());
        return wanted.ToDictionary(id => id, id => found.GetValueOrDefault(id) ?? new ItemRef(id, "A removed item", null));
    }

    /// <summary>Everyone's display name, by user id.</summary>
    public static async Task<IReadOnlyDictionary<Guid, string>> ActorsAsync(IAccountAccess accounts, CancellationToken cancellationToken) =>
        (await accounts.ListUsersAsync(cancellationToken)).ToDictionary(user => user.Id, user => user.DisplayName);
}
