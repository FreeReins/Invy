namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A space, as the person asking sees it.</summary>
/// <param name="Id">The space's id.</param>
/// <param name="Name">Its name, such as Home or Workshop.</param>
/// <param name="Preset">Its kind: the starter pack it was made with (home, hobby, collection, business), or null for none.</param>
/// <param name="Role">The asking person's role in it.</param>
/// <param name="CreatedAt">When it was made.</param>
/// <param name="Features">The keys of the features switched on, such as values and audits. One that is off is hidden, not blocked.</param>
public sealed record SpaceResponse(Guid Id, string Name, string? Preset, SpaceRole Role, DateTimeOffset CreatedAt, IReadOnlyList<string> Features);
