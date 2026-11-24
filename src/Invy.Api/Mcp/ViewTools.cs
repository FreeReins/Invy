using System.ComponentModel;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace Invy.Api.Mcp;

/// <summary>MCP tools for a space's views: saved searches people made in Invy, such as "Not seen in a year".</summary>
[McpServerToolType]
public sealed class ViewTools(IViewManager views, McpCaller caller)
{
    [McpServerTool(Name = "list_views", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the saved views in a space: searches the person or their household or team saved in Invy, such as \"Not seen in a year\" or \"Low batteries\". Use it when someone names a list they keep, or asks what views or saved searches they have. Gives each view's id, name, search text, display and whether it is pinned or shared. Run one with run_view.")]
    public Task<IReadOnlyList<ViewResponse>> ListViews(
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await views.ListViewsAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), cancellationToken));

    [McpServerTool(Name = "run_view", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Runs a saved view's search as of now and gives the items it finds, 50 at a time, each with the containers it is in, in the view's own order. The first page says how many match; pass next as after for the next page. Get the view's id from list_views.")]
    public Task<SearchResponse> RunView(
        [Description("The view's id, from list_views.")] Guid viewId,
        [Description("The next value from the page before, to get the next page.")] string? after = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await views.RunViewAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), viewId, after, cancellationToken));
}
