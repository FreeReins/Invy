namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A QR label and what it is on.</summary>
/// <param name="Code">The short code, such as 7K3Q, unique across the install.</param>
/// <param name="SpaceId">The space it belongs to.</param>
/// <param name="Item">The item it is on, or null while it is unclaimed.</param>
/// <param name="CreatedAt">When the code was made.</param>
/// <param name="PrintedAt">When it was last printed on a sheet, or null if never.</param>
public sealed record LabelResponse(string Code, Guid SpaceId, ItemRef? Item, DateTimeOffset CreatedAt, DateTimeOffset? PrintedAt);
