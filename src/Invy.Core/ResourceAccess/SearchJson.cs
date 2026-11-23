using System.Text.Json.Serialization;

namespace Invy.Core.ResourceAccess;

/// <summary>JSON written into search parameters, made at build time.</summary>
[JsonSerializable(typeof(string))]
internal sealed partial class SearchJson : JsonSerializerContext;
