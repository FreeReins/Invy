using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>A space's projects: what each sets aside, using it up, and finishing. Any member can look; editors change.</summary>
public static class ProjectEndpoints
{
    /// <summary>The space's projects, open ones first; <c>done=true</c> includes finished ones.</summary>
    [WolverineGet("/api/spaces/{spaceId}/projects")]
    public static Task<IReadOnlyList<ProjectResponse>> List(
        Guid spaceId, bool? done, ClaimsPrincipal principal, [FromServices] IProjectManager projects, CancellationToken cancellationToken) =>
        projects.ListProjectsAsync(Sessions.RequireUserId(principal), spaceId, done == true, cancellationToken);

    /// <summary>Starts a project.</summary>
    [WolverinePost("/api/spaces/{spaceId}/projects")]
    public static Task<ProjectResponse> Create(
        Guid spaceId, ProjectRequest request, ClaimsPrincipal principal, [FromServices] IProjectManager projects,
        CancellationToken cancellationToken) =>
        projects.CreateProjectAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>One project, with what it sets aside.</summary>
    [WolverineGet("/api/spaces/{spaceId}/projects/{projectId}")]
    public static Task<ProjectResponse> Get(
        Guid spaceId, Guid projectId, ClaimsPrincipal principal, [FromServices] IProjectManager projects, CancellationToken cancellationToken) =>
        projects.GetProjectAsync(Sessions.RequireUserId(principal), spaceId, projectId, cancellationToken);

    /// <summary>Changes a project's name and notes.</summary>
    [WolverinePut("/api/spaces/{spaceId}/projects/{projectId}")]
    public static Task<ProjectResponse> Update(
        Guid spaceId, Guid projectId, ProjectRequest request, ClaimsPrincipal principal, [FromServices] IProjectManager projects,
        CancellationToken cancellationToken) =>
        projects.UpdateProjectAsync(Sessions.RequireUserId(principal), spaceId, projectId, request, cancellationToken);

    /// <summary>Sets how much of an item the project sets aside (0 to set nothing aside).</summary>
    [WolverinePost("/api/spaces/{spaceId}/projects/{projectId}/reserve")]
    public static Task<ProjectResponse> Reserve(
        Guid spaceId, Guid projectId, ReserveRequest request, ClaimsPrincipal principal, [FromServices] IProjectManager projects,
        CancellationToken cancellationToken) =>
        projects.ReserveAsync(Sessions.RequireUserId(principal), spaceId, projectId, request, cancellationToken);

    /// <summary>Uses up what the project set aside of an item.</summary>
    [WolverinePost("/api/spaces/{spaceId}/projects/{projectId}/use-up")]
    public static Task<ProjectResponse> UseUp(
        Guid spaceId, Guid projectId, UseUpRequest request, ClaimsPrincipal principal, [FromServices] IProjectManager projects,
        CancellationToken cancellationToken) =>
        projects.UseUpAsync(Sessions.RequireUserId(principal), spaceId, projectId, request, cancellationToken);

    /// <summary>Marks the project done, releasing whatever it still sets aside.</summary>
    [WolverinePost("/api/spaces/{spaceId}/projects/{projectId}/finish")]
    public static Task<ProjectResponse> Finish(
        Guid spaceId, Guid projectId, ClaimsPrincipal principal, [FromServices] IProjectManager projects, CancellationToken cancellationToken) =>
        projects.FinishProjectAsync(Sessions.RequireUserId(principal), spaceId, projectId, cancellationToken);

    /// <summary>Removes a project, releasing whatever it still sets aside.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/projects/{projectId}")]
    public static Task Delete(
        Guid spaceId, Guid projectId, ClaimsPrincipal principal, [FromServices] IProjectManager projects, CancellationToken cancellationToken) =>
        projects.DeleteProjectAsync(Sessions.RequireUserId(principal), spaceId, projectId, cancellationToken);

    /// <summary>The open projects that set aside some of this item.</summary>
    [WolverineGet("/api/spaces/{spaceId}/items/{id}/projects")]
    public static Task<IReadOnlyList<ProjectResponse>> ForItem(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IProjectManager projects, CancellationToken cancellationToken) =>
        projects.ListForItemAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);
}
