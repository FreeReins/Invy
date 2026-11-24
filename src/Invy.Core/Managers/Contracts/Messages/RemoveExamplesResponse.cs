namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What removing a space's examples did.</summary>
/// <param name="Removed">How many example items went. One still holding your own things is kept.</param>
public sealed record RemoveExamplesResponse(int Removed);
