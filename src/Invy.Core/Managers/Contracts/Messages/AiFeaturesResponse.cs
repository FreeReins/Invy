namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Which AI features are ready to use, because a model is set up for them.</summary>
/// <param name="Ready">The tasks a model is set up for.</param>
public sealed record AiFeaturesResponse(IReadOnlyList<AiTask> Ready);
