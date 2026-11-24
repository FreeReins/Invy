using System.ComponentModel;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace Invy.Api.Mcp;

/// <summary>MCP tools for a space's item types and their custom fields. Any member can list; editors and owners define.</summary>
[McpServerToolType]
public sealed class TypeTools(ITypeManager types, McpCaller caller)
{
    [McpServerTool(Name = "list_types", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The space's item types, such as Box, Tool or Lego set, by name. Each has an id, whether its items hold other items, and its fields: id, search name (key, used in search and update_item), label, kind, unit and choices. Use it to read get_item's field values, which are by field id, and before giving an item a type.")]
    public Task<IReadOnlyList<ItemTypeResponse>> ListTypes(
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await types.ListTypesAsync(caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), cancellationToken));

    [McpServerTool(Name = "define_type", OpenWorld = false)]
    [Description("Adds a new item type to the space, with its custom fields, such as \"Lego set\" with Set number, Theme and Pieces. Check list_types first: names are unique in a space. Every item already has a name, marking, notes, tags, photos, an amount and a place, so don't add fields for those. Gives back the type with its id and its fields' search names.")]
    public Task<ItemTypeResponse> DefineType(
        [Description("The type's name, such as \"Lego set\".")] string name,
        [Description("True for places and containers (a room, a shelf, a box) whose items hold other items.")] bool holdsItems = false,
        [Description("Its fields, in the order shown.")] IReadOnlyList<NewField>? fields = null,
        [Description(McpCaller.SpaceHelp)] string? space = null,
        CancellationToken cancellationToken = default)
    {
        var definitions = (fields ?? []).Select(field => new FieldDefinition(
            null, string.Empty, field.Label, field.Kind, field.Unit, field.Options ?? [], false, field.Sortable)).ToList();
        return McpErrors.Explain(async () => await types.CreateTypeAsync(
            caller.UserId, await caller.SpaceIdAsync(space, cancellationToken), new ItemTypeRequest(name, null, null, holdsItems, definitions, 0), cancellationToken));
    }
}
