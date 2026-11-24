using System.Text.Json.Serialization;

namespace Invy.Core.Engines.Transformation;

/// <summary>The JSON types made from AI answers, so field values are written without reflection.</summary>
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
internal sealed partial class AiAnswerJson : JsonSerializerContext;
