namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A starter pack of item types that a new space can begin with.</summary>
/// <param name="Key">What to send as <see cref="CreateSpaceRequest.Preset"/>, such as home.</param>
/// <param name="Name">The name shown, such as Home.</param>
/// <param name="Description">One sentence on what it holds.</param>
/// <param name="Types">The names of its types, in order.</param>
public sealed record StarterPackResponse(string Key, string Name, string Description, IReadOnlyList<string> Types);
