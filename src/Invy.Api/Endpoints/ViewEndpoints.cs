using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>
/// Views: saved searches with how to show them, personal or shared with the space. Any member makes their own;
/// editors and owners share them and change shared ones.
/// </summary>
public static class ViewEndpoints
{
    /// <summary>The space's shared views and the caller's own, pinned first, then by name.</summary>
    [WolverineGet("/api/spaces/{spaceId}/views")]
    public static Task<IReadOnlyList<ViewResponse>> List(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] IViewManager views, CancellationToken cancellationToken) =>
        views.ListViewsAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Saves a new view.</summary>
    [WolverinePost("/api/spaces/{spaceId}/views")]
    public static Task<ViewResponse> Create(
        Guid spaceId, ViewRequest request, ClaimsPrincipal principal, [FromServices] IViewManager views, CancellationToken cancellationToken) =>
        views.CreateViewAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Changes a view.</summary>
    [WolverinePut("/api/spaces/{spaceId}/views/{viewId}")]
    public static Task<ViewResponse> Update(
        Guid spaceId, Guid viewId, ViewRequest request, ClaimsPrincipal principal, [FromServices] IViewManager views,
        CancellationToken cancellationToken) =>
        views.UpdateViewAsync(Sessions.RequireUserId(principal), spaceId, viewId, request, cancellationToken);

    /// <summary>Removes a view.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/views/{viewId}")]
    public static Task Delete(
        Guid spaceId, Guid viewId, ClaimsPrincipal principal, [FromServices] IViewManager views, CancellationToken cancellationToken) =>
        views.RemoveViewAsync(Sessions.RequireUserId(principal), spaceId, viewId, cancellationToken);

    /// <summary>Runs a view's search as of now, 50 at a time; <c>after</c> is the last page's <c>next</c>.</summary>
    [WolverineGet("/api/spaces/{spaceId}/views/{viewId}/results")]
    public static Task<SearchResponse> Run(
        Guid spaceId, Guid viewId, string? after, ClaimsPrincipal principal, [FromServices] IViewManager views,
        CancellationToken cancellationToken) =>
        views.RunViewAsync(Sessions.RequireUserId(principal), spaceId, viewId, after, cancellationToken);

    /// <summary>The events of the items the view finds, newest first, 50 at a time; <c>before</c> is the last page's <c>next</c>.</summary>
    [WolverineGet("/api/spaces/{spaceId}/views/{viewId}/timeline")]
    public static Task<ActivityResponse> Timeline(
        Guid spaceId, Guid viewId, string? before, ClaimsPrincipal principal, [FromServices] IViewManager views,
        CancellationToken cancellationToken) =>
        views.ViewTimelineAsync(Sessions.RequireUserId(principal), spaceId, viewId, before, cancellationToken);
}
