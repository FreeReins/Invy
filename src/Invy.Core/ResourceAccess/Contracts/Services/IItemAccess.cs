using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>A space's items, their timelines and their sortable field values. Every call is filtered by space.</summary>
internal interface IItemAccess
{
    /// <summary>One item, or null when the space has none with that id.</summary>
    Task<Item?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>The items with these ids, in any order; ids the space doesn't have are left out.</summary>
    Task<List<Item>> FindManyAsync(Guid spaceId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// The items directly in a container (or at the top when <paramref name="parentId"/> is null), by name then id,
    /// after <paramref name="after"/> (keyset paging), at most <paramref name="limit"/>. Disposed and archived items
    /// are left out. At the top, items in the Inbox (no type and holding nothing) are left out too.
    /// </summary>
    Task<List<Item>> ListChildrenAsync(Guid spaceId, Guid? parentId, ItemCursor? after, int limit, CancellationToken cancellationToken);

    /// <summary>The Inbox: items at the top with no type that hold nothing, by name then id, paged like the above.</summary>
    Task<List<Item>> ListInboxAsync(Guid spaceId, ItemCursor? after, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Inbox items with no suggested type that no model was asked about, or (with <paramref name="askedBefore"/>) was
    /// last asked about before then, oldest first, at most <paramref name="limit"/>.
    /// </summary>
    Task<List<Item>> ListInboxToSuggestAsync(Guid spaceId, DateTimeOffset? askedBefore, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Keeps the type suggested for each item (null for none, or to ignore one) and when it was asked, in one statement
    /// per suggested type, in the request's transaction. Versions don't change, so nobody's edit conflicts.
    /// </summary>
    Task SuggestTypesAsync(Guid spaceId, IReadOnlyDictionary<Guid, Guid?> suggestions, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>How many items (not disposed or archived) are directly in each of these containers.</summary>
    Task<Dictionary<Guid, int>> CountChildrenAsync(Guid spaceId, IReadOnlyCollection<Guid> parentIds, CancellationToken cancellationToken);

    /// <summary>Adds the item, or changes the stored one, and replaces its sortable values with <paramref name="values"/>.</summary>
    Task SaveAsync(Item item, IReadOnlyList<ItemValue> values, CancellationToken cancellationToken);

    /// <summary>
    /// Rewrites the path and depth of everything inside the item at <paramref name="oldPath"/> to sit under
    /// <paramref name="newPath"/>, in one statement in the request's transaction. The item itself is saved by
    /// <see cref="SaveAsync"/>. Nothing else is written for the items inside.
    /// </summary>
    Task MoveContentsAsync(Guid spaceId, string oldPath, string newPath, CancellationToken cancellationToken);

    /// <summary>
    /// The items matching <paramref name="query"/>, in its order, after <paramref name="after"/> (keyset paging: the
    /// last hit of the page before), at most <paramref name="limit"/>.
    /// </summary>
    Task<List<SearchHit>> SearchAsync(Guid spaceId, ItemQuery query, SearchHit? after, int limit, CancellationToken cancellationToken);

    /// <summary>How many items match <paramref name="query"/>: counted up to <paramref name="cap"/>, estimated above it.</summary>
    Task<SearchCount> CountAsync(Guid spaceId, ItemQuery query, int cap, CancellationToken cancellationToken);

    /// <summary>
    /// Recounts how many current items are inside each of these containers at any depth, and their total value, in one
    /// statement. Running it again gives the same result.
    /// </summary>
    Task RecountAsync(Guid spaceId, IReadOnlyCollection<Guid> containerIds, CancellationToken cancellationToken);

    /// <summary>
    /// Sets when these items were last seen, and when <paramref name="contentsOf"/> is given, the current items directly
    /// inside it too, in one statement in the request's transaction. Versions don't change, so nobody's edit conflicts.
    /// </summary>
    Task MarkSeenAsync(Guid spaceId, IReadOnlyCollection<Guid> ids, Guid? contentsOf, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>What the space has so far, apart from its examples, in one query.</summary>
    Task<SpaceProgress> ProgressAsync(Guid spaceId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the space's examples, with their events and values, in one statement in the request's transaction. An
    /// example with something of the person's own inside it, at any depth, is kept. Returns the removed items' paths.
    /// </summary>
    Task<List<string>> RemoveExamplesAsync(Guid spaceId, CancellationToken cancellationToken);

    Task AddEventAsync(ItemEvent itemEvent, CancellationToken cancellationToken);

    /// <summary>An item's own events and what went in and out of it, newest first, at most <paramref name="limit"/>.</summary>
    Task<List<ItemEvent>> ListEventsAsync(Guid spaceId, Guid itemId, int limit, CancellationToken cancellationToken);

    /// <summary>The Created and Moved events of these items, oldest first: where each went, and when.</summary>
    Task<List<ItemEvent>> ListMovesAsync(Guid spaceId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken);

    /// <summary>The space's events, newest first, before <paramref name="before"/> (keyset paging), at most <paramref name="limit"/>.</summary>
    Task<List<ItemEvent>> ListActivityAsync(Guid spaceId, EventCursor? before, int limit, CancellationToken cancellationToken);
}
