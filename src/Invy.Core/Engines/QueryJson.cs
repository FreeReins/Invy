using System.Text.Json.Serialization;

namespace Invy.Core.Engines;

/// <summary>The JSON values QueryEngine writes into field matches, made at build time.</summary>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(bool))]
internal sealed partial class QueryJson : JsonSerializerContext;
