namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One thing that changed in an item, as it read then.</summary>
/// <param name="What">What changed, such as Name or a field's name.</param>
/// <param name="Before">The old value as text, or null when it was empty.</param>
/// <param name="After">The new value as text, or null when it is now empty.</param>
public sealed record ItemChange(string What, string? Before, string? After);
