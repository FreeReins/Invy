using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>
/// QR labels: batches of codes to print, found by scanning, claimed by a new item or stuck on an existing one. Any
/// member can look; editors and owners make and claim labels.
/// </summary>
public static class LabelEndpoints
{
    /// <summary>The label a code or a scanned address names, in whichever space it belongs to.</summary>
    [WolverineGet("/api/labels/{code}")]
    public static Task<LabelResponse> Find(
        string code, ClaimsPrincipal principal, [FromServices] ILabelManager labels, CancellationToken cancellationToken) =>
        labels.FindLabelAsync(Sessions.RequireUserId(principal), code, cancellationToken);

    /// <summary>The space's labels by code, 100 at a time; only unclaimed ones with <c>unclaimed=true</c>.</summary>
    [WolverineGet("/api/spaces/{spaceId}/labels")]
    public static Task<LabelPageResponse> List(
        Guid spaceId, bool? unclaimed, string? after, ClaimsPrincipal principal, [FromServices] ILabelManager labels,
        CancellationToken cancellationToken) =>
        labels.ListLabelsAsync(Sessions.RequireUserId(principal), spaceId, unclaimed ?? false, after, cancellationToken);

    /// <summary>Makes a batch of new unclaimed codes to print.</summary>
    [WolverinePost("/api/spaces/{spaceId}/labels")]
    public static Task<IReadOnlyList<LabelResponse>> Create(
        Guid spaceId, CreateLabelsRequest request, ClaimsPrincipal principal, [FromServices] ILabelManager labels,
        CancellationToken cancellationToken) =>
        labels.CreateLabelsAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Makes a new item (a box named after the code, unless told otherwise) with an unclaimed label on it.</summary>
    [WolverinePost("/api/spaces/{spaceId}/labels/{code}/claim")]
    public static Task<ItemResponse> Claim(
        Guid spaceId, string code, ClaimLabelRequest request, ClaimsPrincipal principal, [FromServices] ILabelManager labels,
        CancellationToken cancellationToken) =>
        labels.ClaimLabelAsync(Sessions.RequireUserId(principal), spaceId, code, request, cancellationToken);

    /// <summary>Sticks an unclaimed label on an existing item.</summary>
    [WolverinePost("/api/spaces/{spaceId}/labels/{code}/attach")]
    public static Task<ItemResponse> Attach(
        Guid spaceId, string code, AttachLabelRequest request, ClaimsPrincipal principal, [FromServices] ILabelManager labels,
        CancellationToken cancellationToken) =>
        labels.AttachLabelAsync(Sessions.RequireUserId(principal), spaceId, code, request, cancellationToken);
}
