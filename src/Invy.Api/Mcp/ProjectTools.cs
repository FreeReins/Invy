using System.ComponentModel;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace Invy.Api.Mcp;

/// <summary>MCP tools for projects: what each sets aside, and using it up. Viewers can list; editors and owners change.</summary>
[McpServerToolType]
public sealed class ProjectTools(IProjectManager projects, McpCaller caller)
{
    [McpServerTool(Name = "list_projects", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the space's open projects (such as Build a bookshelf), each with its id and the things it has set aside and how much. Set done to include finished ones.")]
    public Task<IReadOnlyList<ProjectResponse>> ListProjects(
        [Description("True to include finished projects.")] bool done = false,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await projects.ListProjectsAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), done, cancellationToken));

    [McpServerTool(Name = "start_project", OpenWorld = false)]
    [Description("Starts a project, so things can be set aside for it with reserve_for_project. Gives back its id.")]
    public Task<ProjectResponse> StartProject(
        [Description("What the project is, such as Build a bookshelf.")] string name,
        [Description("Notes about it, or leave it out.")] string? notes = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await projects.CreateProjectAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), new ProjectRequest(name, notes, 0), cancellationToken));

    [McpServerTool(Name = "reserve_for_project", Idempotent = true, OpenWorld = false)]
    [Description("Sets aside some of a thing for a project (\"keep 20 screws for the bookshelf\"), replacing what was set aside before; 0 sets nothing aside. The amount doesn't change until use_up_for_project, but what's set aside counts towards running low. A single (not counted) thing is set aside whole, as 1. Find the item with where_is and the project with list_projects.")]
    public Task<ProjectResponse> ReserveForProject(
        [Description("The project's id.")] Guid projectId,
        [Description("The item's id.")] Guid itemId,
        [Description("How much to set aside: 1 for a single thing, or 0 to set nothing aside.")] decimal amount,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await projects.ReserveAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), projectId, new ReserveRequest(itemId, amount), cancellationToken));

    [McpServerTool(Name = "use_up_for_project", OpenWorld = false)]
    [Description("Records that a project used what it set aside (\"I used 12 of the screws\"): the amount comes off a counted thing, and a single thing is marked disposed of. Leave amount out to use all that was set aside.")]
    public Task<ProjectResponse> UseUpForProject(
        [Description("The project's id.")] Guid projectId,
        [Description("The item's id.")] Guid itemId,
        [Description("How much was used, up to what was set aside. Leave it out for all of it.")] decimal? amount = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await projects.UseUpAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), projectId, new UseUpRequest(itemId, amount), cancellationToken));
}
