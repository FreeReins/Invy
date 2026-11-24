namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One step on the getting-started card.</summary>
/// <param name="Key">Which step, such as places or labels.</param>
/// <param name="Title">What the card shows, such as Print labels.</param>
/// <param name="Description">One plain line on how to do it.</param>
/// <param name="Done">True once the space shows it was done.</param>
public sealed record GettingStartedStep(string Key, string Title, string Description, bool Done);
