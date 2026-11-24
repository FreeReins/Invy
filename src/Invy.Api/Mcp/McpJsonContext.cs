using System.Text.Json;
using System.Text.Json.Serialization;
using Invy.Core.Managers.Contracts;
using ModelContextProtocol;

namespace Invy.Api.Mcp;

/// <summary>
/// The JSON shapes of the MCP tools' own parameters, made at build time. Their results are Invy's contracts,
/// read with <see cref="ContractJsonContext"/>, and the protocol's own messages use the SDK's context.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(DateOnly))]
[JsonSerializable(typeof(DateOnly?))]
[JsonSerializable(typeof(decimal?))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(IReadOnlyList<Guid>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, string>))]
[JsonSerializable(typeof(IReadOnlyList<NewField>))]
[JsonSerializable(typeof(decimal))]
public sealed partial class McpJsonContext : JsonSerializerContext
{
    /// <summary>Options for the tools: their parameters, Invy's contracts, then the protocol's own types.</summary>
    public static JsonSerializerOptions ToolOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
            ContractJsonContext.Default, Default, McpJsonUtilities.DefaultOptions.TypeInfoResolver),
    };
}
