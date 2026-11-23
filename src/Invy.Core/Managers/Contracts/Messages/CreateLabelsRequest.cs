namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Asks for a batch of new, unclaimed label codes to print.</summary>
/// <param name="Count">How many, from 1 to 500. A sheet holds 30 or 65.</param>
public sealed record CreateLabelsRequest(int Count);
