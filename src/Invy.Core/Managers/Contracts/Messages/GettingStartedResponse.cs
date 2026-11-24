namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>The getting-started card of a space, for the person asking.</summary>
/// <param name="Steps">The kind's steps, in order, each ticked from what the space already has.</param>
/// <param name="ExamplesLeft">How many example items are left to remove.</param>
/// <param name="Dismissed">True once the person put the card away. Each member has their own.</param>
public sealed record GettingStartedResponse(IReadOnlyList<GettingStartedStep> Steps, int ExamplesLeft, bool Dismissed);
