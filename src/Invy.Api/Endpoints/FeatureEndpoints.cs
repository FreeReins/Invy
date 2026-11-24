using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>
/// The space features' item calls: low stock, lending, reminder dates and moving. They work whether or not the
/// space has the feature switched on; the switch only hides it in the page. Any member can look; editors change.
/// </summary>
public static class FeatureEndpoints
{
    /// <summary>Counted items at or below their low-stock level, after what projects set aside.</summary>
    [WolverineGet("/api/spaces/{spaceId}/low-stock")]
    public static Task<ItemListResponse> LowStock(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.ListLowStockAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Sets or clears a counted item's low-stock level.</summary>
    [WolverinePut("/api/spaces/{spaceId}/items/{id}/low-stock")]
    public static Task<ItemResponse> SetLowStock(
        Guid spaceId, Guid id, LowStockRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.SetLowStockAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>What is lent out, by due date; <c>overdue=true</c> for only what is past its date.</summary>
    [WolverineGet("/api/spaces/{spaceId}/loans")]
    public static Task<ItemListResponse> Loans(
        Guid spaceId, bool? overdue, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.ListLoansAsync(Sessions.RequireUserId(principal), spaceId, overdue == true, cancellationToken);

    /// <summary>Lends an item out, whole, to a person or a place.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/lend")]
    public static Task<ItemResponse> Lend(
        Guid spaceId, Guid id, LendRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.LendAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Marks a lent item back.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/return")]
    public static Task<ItemResponse> Return(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.ReturnAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Reminder dates within <c>days</c> (30 by default) and overdue upkeep, soonest first.</summary>
    [WolverineGet("/api/spaces/{spaceId}/reminders")]
    public static Task<RemindersResponse> Reminders(
        Guid spaceId, int? days, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.ListRemindersAsync(Sessions.RequireUserId(principal), spaceId, days, cancellationToken);

    /// <summary>Marks an upkeep date done: it moves on from today by its field's repeat.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/upkeep-done")]
    public static Task<ItemResponse> UpkeepDone(
        Guid spaceId, Guid id, UpkeepDoneRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.MarkUpkeepDoneAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Boxes and things with a moving destination, by name.</summary>
    [WolverineGet("/api/spaces/{spaceId}/moving")]
    public static Task<ItemListResponse> Moving(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.ListGoingAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Sets or clears where a box goes in a move.</summary>
    [WolverinePut("/api/spaces/{spaceId}/items/{id}/destination")]
    public static Task<ItemResponse> SetDestination(
        Guid spaceId, Guid id, DestinationRequest request, ClaimsPrincipal principal, [FromServices] IItemManager items,
        CancellationToken cancellationToken) =>
        items.SetDestinationAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Moves a box into its destination with one Moved event, and clears the destination.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/arrived")]
    public static Task<ItemResponse> Arrived(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.MarkArrivedAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>A box's manifest: where it goes and what is directly inside it.</summary>
    [WolverineGet("/api/spaces/{spaceId}/items/{id}/manifest")]
    public static Task<ManifestResponse> Manifest(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IItemManager items, CancellationToken cancellationToken) =>
        items.GetManifestAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);
}
