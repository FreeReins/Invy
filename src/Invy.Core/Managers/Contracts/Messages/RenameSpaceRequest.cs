namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A new name for a space.</summary>
/// <param name="Name">The name.</param>
public sealed record RenameSpaceRequest(string Name);
