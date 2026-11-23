using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// A job Invy gives an AI model. Each one uses the default model unless another is chosen for it. The tasks come
/// with the features that use them (see the <c>add-ai-task</c> skill).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AiTask>))]
public enum AiTask
{
}
