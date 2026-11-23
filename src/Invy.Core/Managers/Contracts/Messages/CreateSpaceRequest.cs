namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A new space. The person making it becomes its owner.</summary>
/// <param name="Name">Its name, such as Home or Workshop.</param>
/// <param name="Preset">A starter pack of types to begin with (home, hobby, business), or null for none.</param>
public sealed record CreateSpaceRequest(string Name, string? Preset);
