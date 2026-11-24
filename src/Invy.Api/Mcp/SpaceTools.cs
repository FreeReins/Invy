using System.ComponentModel;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace Invy.Api.Mcp;

/// <summary>MCP tools for the spaces the token's owner is in.</summary>
[McpServerToolType]
public sealed class SpaceTools(ISpaceManager spaces, McpCaller caller)
{
    [McpServerTool(Name = "list_spaces", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The spaces (separate inventories, such as Home or Workshop) this person is in, with their role in each: Viewer can look, Editor and Owner can change. Every other tool takes one of these as space; when the person is in only one, it can be left out.")]
    public Task<IReadOnlyList<SpaceResponse>> ListSpaces(CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => spaces.ListSpacesAsync(caller.UserId, cancellationToken));
}
