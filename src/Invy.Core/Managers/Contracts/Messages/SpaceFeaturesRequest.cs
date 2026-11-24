namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>The features a space should have on. Any left out are switched off.</summary>
/// <param name="Features">Their keys, such as values and audits.</param>
public sealed record SpaceFeaturesRequest(IReadOnlyList<string> Features);
