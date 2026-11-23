using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>
/// Works out where an item has been. Its location at any moment is the container its own last move put it in, inside
/// wherever that container's last move had put it by then, and so on up. So moving box F to the garage moves the
/// baseball in it too, without an event for the baseball.
/// </summary>
internal sealed class TimelineEngine
{
    /// <summary>The deepest chain followed, so a damaged history can't loop forever.</summary>
    private const int MaxDepth = 64;

    /// <summary>
    /// The containers that any of these moves put something in. Called again with the moves of those containers until
    /// nothing new turns up, it finds every container an item was ever inside.
    /// </summary>
    public IReadOnlySet<Guid> ContainersIn(IEnumerable<ItemEvent> moves) =>
        moves.Select(move => move.ToParent).OfType<Guid>().ToHashSet();

    /// <summary>
    /// The places <paramref name="itemId"/> has been, oldest first: each time its chain of containers changed, by its
    /// own moves or by its containers' moves while it was inside them. <paramref name="moves"/> holds the Created and
    /// Moved events of the item and of every container it was ever inside, at any depth.
    /// </summary>
    public IReadOnlyList<Stay> Places(Guid itemId, IEnumerable<ItemEvent> moves)
    {
        var byItem = moves
            .Where(move => move.Kind is ItemEventKind.Created or ItemEventKind.Moved)
            .GroupBy(move => move.ItemId)
            .ToDictionary(group => group.Key, group => group.OrderBy(move => move.At).ThenBy(move => move.Id).ToList());
        if (!byItem.TryGetValue(itemId, out var own) || own.Count == 0)
        {
            return [];
        }

        var start = own[0].At;
        var moments = byItem.Values.SelectMany(list => list).Select(move => move.At)
            .Where(at => at >= start).Distinct().Order().ToList();

        var stays = new List<Stay>();
        foreach (var at in moments)
        {
            var chain = Chain(itemId, at, byItem);
            if (stays.Count > 0 && stays[^1].Chain.SequenceEqual(chain))
            {
                continue;
            }

            if (stays.Count > 0)
            {
                stays[^1] = stays[^1] with { Until = at };
            }

            stays.Add(new Stay(at, null, chain));
        }

        return stays;
    }

    /// <summary>The containers the item was in at <paramref name="at"/>, top first, or null if it didn't exist yet.</summary>
    public IReadOnlyList<Guid>? LocationAt(IReadOnlyList<Stay> places, DateTimeOffset at) =>
        places.LastOrDefault(stay => stay.From <= at)?.Chain;

    private static List<Guid> Chain(Guid itemId, DateTimeOffset at, Dictionary<Guid, List<ItemEvent>> byItem)
    {
        var chain = new List<Guid>();
        var current = itemId;
        while (chain.Count < MaxDepth && ParentAt(current, at, byItem) is Guid parent && !chain.Contains(parent) && parent != itemId)
        {
            chain.Add(parent);
            current = parent;
        }

        chain.Reverse();
        return chain;
    }

    /// <summary>Where the item's last move at or before <paramref name="at"/> put it; null at the top or when unknown.</summary>
    private static Guid? ParentAt(Guid itemId, DateTimeOffset at, Dictionary<Guid, List<ItemEvent>> byItem) =>
        byItem.TryGetValue(itemId, out var list) ? list.LastOrDefault(move => move.At <= at)?.ToParent : null;

    /// <summary>A time spent inside one chain of containers (ids, top first).</summary>
    internal sealed record Stay(DateTimeOffset From, DateTimeOffset? Until, IReadOnlyList<Guid> Chain);
}
