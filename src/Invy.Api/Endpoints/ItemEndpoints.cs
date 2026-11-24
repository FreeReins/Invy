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

    /// <summary>Changes only what the request names; field values are text by search name. No version needed.</summary>
    [WolverinePatch("/api/spaces/{spaceId}/items/{id}")]
    public static Task<ItemResponse> Change(
        Guid spaceId, Guid id, ItemChangesRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.ChangeItemAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Moves several items, each with what's inside it, into one container or the top.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/move")]
    public static Task<IReadOnlyList<ItemResponse>> MoveMany(
        Guid spaceId, MoveItemsRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.MoveItemsAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Where the best 10 matches for <c>q</c> are, with their containers as text.</summary>
    [WolverineGet("/api/spaces/{spaceId}/where-is")]
    public static Task<WhereIsResponse> WhereIs(
        Guid spaceId, string? q, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.WhereIsAsync(Sessions.RequireUserId(principal), spaceId, q ?? string.Empty, cancellationToken);

    /// <summary>Asks the AI model what is in photos of an open box. Only suggestions come back; nothing is added.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/from-photo")]
    public static Task<ItemSuggestionsResponse> FromPhoto(
        Guid spaceId, PhotoRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.SuggestFromPhotoAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Asks the AI model to read an item's fields from photos of a label or nameplate. Nothing is saved.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/details-from-photo")]
    public static Task<DetailSuggestionsResponse> DetailsFromPhoto(
        Guid spaceId, Guid id, PhotoRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.SuggestDetailsFromPhotoAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Asks for types for the Inbox items, in the background. Answers 202; suggestions show in the Inbox as they come.</summary>
    [WolverinePost("/api/spaces/{spaceId}/inbox/suggest-types")]
    public static async Task<IResult> SuggestInboxTypes(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken)
    {
        await items.SuggestInboxTypesAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);
        return Results.Accepted();
    }

    /// <summary>Gives an Inbox item the type suggested for it.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/suggested-type/accept")]
    public static Task<ItemResponse> AcceptSuggestedType(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.AcceptSuggestedTypeAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Drops the type suggested for an Inbox item.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/suggested-type/ignore")]
    public static Task<ItemResponse> IgnoreSuggestedType(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.IgnoreSuggestedTypeAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

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
