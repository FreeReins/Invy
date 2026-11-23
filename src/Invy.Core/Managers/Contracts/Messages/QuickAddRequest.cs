namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Several items typed in one go.</summary>
/// <param name="Text">Names separated by commas or new lines, with amounts where needed: "baseball, glove, 2 bats, 3.5 kg filament".</param>
/// <param name="ParentId">The container to put them in, or null for the Inbox.</param>
public sealed record QuickAddRequest(string Text, Guid? ParentId);
