using System.ComponentModel;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Invy.Api.Mcp;

/// <summary>
/// MCP tools for finding things in a space and changing them: places, containers and things, nested to any depth.
/// Viewers of a space can find; editors and owners change.
/// </summary>
[McpServerToolType]
public sealed class ItemTools(IItemManager items, McpCaller caller)
{
    // ---------- Finding ----------

    [McpServerTool(Name = "where_is", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Answers \"where is my …?\": finds the things matching some words and says where each one is, as the chain of containers it is in (\"Garage › Shelf 2 › Box F\"). Use this first whenever someone asks where something is or wants to do something with a thing they name. Gives up to 10 best matches with their ids, status (here, missing, lent), amount and when last seen, and how many matched in all.")]
    public Task<WhereIsResponse> WhereIs(
        [Description("What to look for, in the person's words, such as \"baseball\" or \"red drill\". Search syntax works too, such as type:tool brand:dewalt.")] string what,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.WhereIsAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), what, cancellationToken));

    [McpServerTool(Name = "find_items", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Searches a space's items, 50 at a time, each with the containers it is in. Words match names, markings, tags, notes and field values. Filters: type:tool, in:\"Box F\" (anything inside, at any depth), brand:dewalt (a field by its search name), price>50 and bought>2026-01-31 (sortable fields), qty<5, seen>1y (not seen in a year), added<30d, is:container, is:counted, is:missing, is:lent, is:disposed, has:barcode, tag:garden. Disposed items are left out unless asked for. sort is relevance, name or recent. The first page says how many match; pass next as after for the next page.")]
    public Task<SearchResponse> FindItems(
        [Description("The search, such as \"type:tool in:garage\" or \"batteries qty<4\". Empty lists the most recently changed.")] string? query = null,
        [Description("relevance (the default with words), name (the default without) or recent.")] string? sort = null,
        [Description("The next value from the page before, to get the next page.")] string? after = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.SearchAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), query, sort, after, cancellationToken));

    [McpServerTool(Name = "get_item", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Everything about one item: its name, marking, type, field values (by field id; list_types names them), tags, notes, amount, status, when last seen, the chain of containers it is in, and how many things are directly inside it.")]
    public Task<ItemDetailResponse> GetItem(
        [Description("The item's id, from where_is, find_items or list_contents.")] Guid itemId,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.GetItemAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, cancellationToken));

    [McpServerTool(Name = "list_contents", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("What is directly inside a container (one level, by name, 100 at a time), or the places at the top of the space when no container is given. Each entry says whether it holds items and how many are inside. Pass next as after for the next page. To list everything inside at any depth, use find_items with in:\"<name>\".")]
    public Task<ItemPageResponse> ListContents(
        [Description("The container's id, or leave it out for the top of the space.")] Guid? containerId = null,
        [Description("The next value from the page before.")] string? after = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.ListContentsAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), containerId, after, cancellationToken));

    [McpServerTool(Name = "item_history", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("An item's timeline (added, changed, moved, counted, seen, lent, disposed, newest first, with who did it) and every place it has been with dates, counting moves of the containers it was in. Use it for \"when did I last move …\" or \"where was … before\".")]
    public Task<ItemHistoryResponse> ItemHistory(
        [Description("The item's id.")] Guid itemId,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.GetHistoryAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, cancellationToken));

    // ---------- Changing ----------

    [McpServerTool(Name = "add_items", OpenWorld = false)]
    [Description("Adds things, several at once, inside a container or (without one) to the space's Inbox to sort later. Write them as a list: \"baseball, glove, 2 bats, 3.5 kg filament\"; a number in front, a unit, or \"x200\" after makes a counted item. Find the container's id with where_is first. Gives back the items made; use update_item afterwards to set a type or fields.")]
    public Task<IReadOnlyList<ItemResponse>> AddItems(
        [Description("The things to add, separated by commas or new lines, such as \"baseball, glove, 2 bats\". Up to 50.")] string names,
        [Description("The container to put them in, from where_is or list_contents. Leave it out for the Inbox.")] Guid? containerId = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.QuickAddAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), new QuickAddRequest(names, containerId), cancellationToken));

    [McpServerTool(Name = "update_item", Idempotent = true, OpenWorld = false)]
    [Description("Changes an item's name, marking, type, notes, tags or field values. Only what is given changes. Field values go by the field's search name (from list_types), written as text: \"Makita\", \"129.99\", \"2024-03-01\", \"yes\"; an empty value clears the field. To move it use move_items; to change an amount use adjust_quantity.")]
    public Task<ItemResponse> UpdateItem(
        [Description("The item's id.")] Guid itemId,
        [Description("A new name.")] string? name = null,
        [Description("What's written on it, such as ABC. Empty clears it.")] string? marking = null,
        [Description("A new type's id, from list_types.")] Guid? typeId = null,
        [Description("New notes. Empty clears them.")] string? notes = null,
        [Description("The tags it should have, replacing the old ones.")] IReadOnlyList<string>? tags = null,
        [Description("Field values by search name, such as {\"brand\": \"Makita\", \"price\": \"129.99\"}.")] IReadOnlyDictionary<string, string>? fields = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.ChangeItemAsync(
            caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId,
            new ItemChangesRequest(name, marking, typeId, notes, tags, fields), cancellationToken));

    [McpServerTool(Name = "move_items", Idempotent = true, OpenWorld = false)]
    [Description("Puts things into a container, such as \"put the glove in box F\": find both with where_is, then pass the things' ids and the container's id. Everything inside a moved thing goes with it. Set toTop instead of a container to take them out to the top of the space. Gives back the items as they are now.")]
    public Task<IReadOnlyList<ItemResponse>> MoveItems(
        [Description("The ids of the things to move, 1 to 100.")] IReadOnlyList<Guid> itemIds,
        [Description("The id of the container they go in.")] Guid? containerId = null,
        [Description("True to take them out of any container, to the top of the space.")] bool toTop = false,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        containerId is null && !toTop
            ? throw new McpException("Say where they go: a containerId, or toTop to take them out to the top of the space.")
            : McpErrors.Explain(async () => await items.MoveItemsAsync(
                caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), new MoveItemsRequest(itemIds, toTop ? null : containerId), cancellationToken));

    [McpServerTool(Name = "adjust_quantity", OpenWorld = false)]
    [Description("Adds to or takes from a counted item's amount, such as \"I used 3 batteries\" (delta -3) or \"bought 2 more\" (delta 2). Only counted items have an amount; get_item shows it. The amount can't go below zero.")]
    public Task<ItemResponse> AdjustQuantity(
        [Description("The counted item's id.")] Guid itemId,
        [Description("How much to add, or negative to take away.")] decimal delta,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.AdjustQuantityAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, new AdjustQuantityRequest(delta), cancellationToken));

    [McpServerTool(Name = "mark_seen", Idempotent = true, OpenWorld = false)]
    [Description("Records that someone has just seen an item where Invy says it is, such as \"the drill is in box F, I'm looking at it\". For a container, what is directly inside it is marked seen too. Marking the same thing again within 10 minutes changes nothing.")]
    public Task<ItemResponse> MarkSeen(
        [Description("The item's id.")] Guid itemId,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.MarkSeenAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, cancellationToken));
}
