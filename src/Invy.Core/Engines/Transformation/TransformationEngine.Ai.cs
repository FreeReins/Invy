using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines.Transformation;

/// <content>Questions for AI models, and reading their answers.</content>
internal sealed partial class TransformationEngine
{
    /// <summary>A short question that only checks a model answers, and answers in JSON.</summary>
    public static AiQuestion AiTestQuestion() => new(
        "You are checking that a connection works. Answer only with the JSON asked for.",
        "Set ok to true.",
        [],
        """{"type":"object","properties":{"ok":{"type":"boolean"}},"required":["ok"],"additionalProperties":false}""",
        "connection_check");
}
