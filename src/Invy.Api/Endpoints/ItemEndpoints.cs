using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>
/// A space's items: places, containers and things, nested to any depth, and their timelines. Any member can look;
/// editors and owners add, change and move.
/// </summary>
public static class ItemEndpoints
{
    /// <summary>What is directly inside a container (<c>parent</c>), or at the top without one, by name, 100 at a time.</summary>
    [WolverineGet("/api/spaces/{spaceId}/items")]
    public static Task<ItemPageResponse> Contents(
        Guid spaceId, Guid? parent, string? after, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.ListContentsAsync(Sessions.RequireUserId(principal), spaceId, parent, after, cancellationToken);

    /// <summary>The Inbox: items with no place and no type, by name, 100 at a time.</summary>
    [WolverineGet("/api/spaces/{spaceId}/inbox")]
    public static Task<ItemPageResponse> Inbox(
        Guid spaceId, string? after, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.ListInboxAsync(Sessions.RequireUserId(principal), spaceId, after, cancellationToken);

    /// <summary>An item with the chain of containers it is in.</summary>
    [WolverineGet("/api/spaces/{spaceId}/items/{id}")]
    public static Task<ItemDetailResponse> Get(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.GetItemAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Adds an item.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items")]
    public static Task<ItemResponse> Create(
        Guid spaceId, ItemRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.CreateItemAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Changes an item's details and fields. Send the version you opened; 409 if someone changed it since.</summary>
    [WolverinePut("/api/spaces/{spaceId}/items/{id}")]
    public static Task<ItemResponse> Update(
        Guid spaceId, Guid id, ItemRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.UpdateItemAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Moves an item, and everything inside it, into another container or to the top.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/move")]
    public static Task<ItemResponse> Move(
        Guid spaceId, Guid id, MoveItemRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.MoveItemAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Adds to or takes from a counted item's amount.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/quantity")]
    public static Task<ItemResponse> AdjustQuantity(
        Guid spaceId, Guid id, AdjustQuantityRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.AdjustQuantityAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Marks an item thrown away, sold or given away. Refused while anything is inside it.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/dispose")]
    public static Task<ItemResponse> Dispose(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.DisposeItemAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Brings a disposed or archived item back.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/restore")]
    public static Task<ItemResponse> Restore(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.RestoreItemAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Looks a UPC or EAN barcode up online, once an admin has turned that on, to fill in a new item.</summary>
    [WolverineGet("/api/spaces/{spaceId}/barcodes/{code}")]
    public static Task<ProductResponse> LookUpBarcode(
        Guid spaceId, string code, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.LookUpBarcodeAsync(Sessions.RequireUserId(principal), spaceId, code, cancellationToken);

    /// <summary>Marks an item seen, and what is directly inside it. The web page calls it when a container is opened or scanned.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/seen")]
    public static Task<ItemResponse> Seen(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.MarkSeenAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Finishes an audit walk: what isn't ticked is marked missing, and the differences are listed.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/audit")]
    public static Task<AuditResponse> Audit(
        Guid spaceId, Guid id, AuditRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.AuditAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>An item's timeline and where it has been.</summary>
    [WolverineGet("/api/spaces/{spaceId}/items/{id}/history")]
    public static Task<ItemHistoryResponse> History(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.GetHistoryAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Adds several items typed in one line, such as "baseball, glove, 2 bats", in a container or the Inbox.</summary>
    [WolverinePost("/api/spaces/{spaceId}/quick-add")]
    public static Task<IReadOnlyList<ItemResponse>> QuickAdd(
        Guid spaceId, QuickAddRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.QuickAddAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>
    /// Finds items by what you type (<c>q</c>), such as <c>drill type:tool in:shed price&gt;50</c>, 50 at a time.
    /// <c>sort</c> is relevance, name or recent. The first page says how many match.
    /// </summary>
    [WolverineGet("/api/spaces/{spaceId}/search")]
    public static Task<SearchResponse> Search(
        Guid spaceId, string? q, string? sort, string? after, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.SearchAsync(Sessions.RequireUserId(principal), spaceId, q, sort, after, cancellationToken);

    /// <summary>The space's timeline, newest first, 50 at a time.</summary>
    [WolverineGet("/api/spaces/{spaceId}/activity")]
    public static Task<ActivityResponse> Activity(
        Guid spaceId, string? before, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.ListActivityAsync(Sessions.RequireUserId(principal), spaceId, before, cancellationToken);
}
