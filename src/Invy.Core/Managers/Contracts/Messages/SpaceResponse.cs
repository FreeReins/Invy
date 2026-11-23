namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A space, as the person asking sees it.</summary>
/// <param name="Id">The space's id.</param>
/// <param name="Name">Its name, such as Home or Workshop.</param>
/// <param name="Preset">The starter pack of types it was made with (home, hobby, business), or null for none.</param>
/// <param name="Role">The asking person's role in it.</param>
/// <param name="CreatedAt">When it was made.</param>
public sealed record SpaceResponse(Guid Id, string Name, string? Preset, SpaceRole Role, DateTimeOffset CreatedAt);
