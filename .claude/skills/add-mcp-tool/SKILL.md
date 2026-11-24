---
name: add-mcp-tool
description: "Add or change an MCP tool on Invy's /mcp server so Claude and other assistants can use a feature. USE FOR: new MCP tool, expose a feature to assistants, change a tool's description or parameters. DO NOT USE FOR: the manager operation behind it (add-core-operation first), AI model calls Invy makes itself (add-ai-task)."
---

# Add an MCP tool

A tool is a thin wrapper that calls one manager method, like an endpoint. Add the manager method first with
`add-core-operation`.

1. **Class**: `src/Invy.Api/Mcp/<Area>Tools.cs`, a `[McpServerToolType] sealed class` that takes the manager and
   `McpCaller` (the token's owner, and the `space` argument) in its constructor. Use an existing class when the area fits.
2. **Method**: `[McpServerTool(Name = "verb_noun", ReadOnly = …, Idempotent = …, Destructive = …, OpenWorld = false)]`
   with `[Description]` on the method and every parameter. Write descriptions for an assistant that knows nothing
   about Invy: what it does, when to use it, and what comes back. Give `space` the description `McpCaller.SpaceHelp`.
3. **Body**: one call wrapped in `McpErrors.Explain(async () => await manager.XAsync(caller.UserId, await
   caller.SpaceIdAsync(space, ct), ...))`, so an `InvyException` reaches the
   assistant as its plain message. Look up names (a container called "Box F") through the manager, never in the tool.
4. **Answer**: return a contract record from `Managers/Contracts/Messages/`. Keep it small: ids, names, the location
   chain as text, and a count when the list is cut short. Add the type to `McpJsonContext.cs`.
5. **Register**: `.WithTools<<Area>Tools>(McpJsonContext.ToolOptions)` in `Program.cs` beside the others.
6. **Docs**: the tools table in `docs/admin/ai-assistants.md`, and the MCP tools table in
   `docs/developers/design.md` if it's a new tool.
7. **Check**: the build, and a test in `McpToolTests.cs` that calls the tool as an assistant would (its `Tools()`
   helper signs in as a TestCore user).

## Example

`where_is` in `ItemTools.cs`: calls `IItemManager.WhereIsAsync`, answers each match with its containers as text.

## Troubleshooting

- The assistant never picks the tool: the description says what it is, not when to use it.
- 401 on `/mcp`: the token is missing or revoked. Cookies don't work on `/mcp`.
