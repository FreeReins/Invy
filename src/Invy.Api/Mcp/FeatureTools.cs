using System.ComponentModel;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace Invy.Api.Mcp;

/// <summary>
/// MCP tools for the space features: low stock, lending, warranty and upkeep reminders, and moving. They work whether
/// or not the space shows the feature. Viewers can list; editors and owners change.
/// </summary>
[McpServerToolType]
public sealed class FeatureTools(IItemManager items, McpCaller caller)
{
    // ---------- Stock ----------

    [McpServerTool(Name = "list_low_stock", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the counted things that are running low: at or below their low-stock level, after what projects have set aside. Use it for \"what do I need to buy?\" or \"what's running low?\". Gives each item's id, name, amount and unit, by name.")]
    public Task<ItemListResponse> ListLowStock(
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.ListLowStockAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), cancellationToken));

    [McpServerTool(Name = "set_low_stock", Idempotent = true, OpenWorld = false)]
    [Description("Sets the amount at or below which a counted thing is running low, so the person is told when it gets there. Leave level out to stop watching it. Only counted things (with an amount) can have a level; find the item's id with where_is first.")]
    public Task<ItemResponse> SetLowStock(
        [Description("The item's id.")] Guid itemId,
        [Description("The level, such as 5, in the item's own unit. Leave it out to clear it.")] decimal? level = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.SetLowStockAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, new LowStockRequest(level), cancellationToken));

    // ---------- Lending ----------

    [McpServerTool(Name = "list_loans", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists what is lent out, with who or where has each thing (lentTo) and when it is due back (dueOn), soonest first. Set overdue to list only what is past its due date. Use it for \"who has my drill?\" or \"what's overdue?\".")]
    public Task<ItemListResponse> ListLoans(
        [Description("True for only loans past their due date.")] bool overdue = false,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.ListLoansAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), overdue, cancellationToken));

    [McpServerTool(Name = "lend_item", OpenWorld = false)]
    [Description("Marks a thing as lent to a person or a place (\"I lent the drill to Sam until Friday\"), with an optional due date. The whole thing is lent. Find its id with where_is first. Use return_item when it comes back.")]
    public Task<ItemResponse> LendItem(
        [Description("The item's id.")] Guid itemId,
        [Description("Who or where it went, as the person said it, such as Sam or the church hall.")] string to,
        [Description("When it should come back, as yyyy-MM-dd, or leave it out for no date.")] DateOnly? dueOn = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.LendAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, new LendRequest(to, dueOn), cancellationToken));

    [McpServerTool(Name = "return_item", Idempotent = true, OpenWorld = false)]
    [Description("Marks a lent thing as back. Nothing changes if it wasn't lent.")]
    public Task<ItemResponse> ReturnItem(
        [Description("The item's id, from list_loans or where_is.")] Guid itemId,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.ReturnAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, cancellationToken));

    // ---------- Reminders ----------

    [McpServerTool(Name = "list_reminders", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists warranty, upkeep and other reminder dates coming up within some days (30 by default), and upkeep that is overdue, soonest first. Each has the item, the field's name and id, the date, how many days away it is (negative when passed) and whether it is upkeep that mark_upkeep_done can tick off.")]
    public Task<RemindersResponse> ListReminders(
        [Description("How many days ahead to look, 0 to 365. 30 when left out.")] int? days = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.ListRemindersAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), days, cancellationToken));

    [McpServerTool(Name = "mark_upkeep_done", OpenWorld = false)]
    [Description("Records that upkeep was done today (\"I serviced the boiler\"): its date moves on from today by the field's repeat. Only upkeep dates (upkeep: true in list_reminders) can be marked done.")]
    public Task<ItemResponse> MarkUpkeepDone(
        [Description("The item's id, from list_reminders.")] Guid itemId,
        [Description("The upkeep field's id, from list_reminders.")] Guid fieldId,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.MarkUpkeepDoneAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, new UpkeepDoneRequest(fieldId), cancellationToken));

    // ---------- Moving ----------

    [McpServerTool(Name = "set_destination", Idempotent = true, OpenWorld = false)]
    [Description("For a house move: says which room or container a packed box goes to in the new place (\"box 4 goes to the kitchen\"). The destination must be a place or container in the same space; find both ids with where_is. Leave destinationId out to clear it.")]
    public Task<ItemResponse> SetDestination(
        [Description("The box's id.")] Guid itemId,
        [Description("The room or container it goes to, or leave it out to clear it.")] Guid? destinationId = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.SetDestinationAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, new DestinationRequest(destinationId), cancellationToken));

    [McpServerTool(Name = "mark_arrived", OpenWorld = false)]
    [Description("For a house move: a box has arrived, so it moves into its destination (with everything inside it) and the destination is cleared. The box must have a destination; use move_items otherwise.")]
    public Task<ItemResponse> MarkArrived(
        [Description("The box's id.")] Guid itemId,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await items.MarkArrivedAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), itemId, cancellationToken));
}
