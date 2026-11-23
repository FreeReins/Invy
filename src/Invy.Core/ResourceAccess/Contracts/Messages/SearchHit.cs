namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>One search result, with what it was sorted by, so the next page can start after it.</summary>
/// <param name="Id">The item's id.</param>
/// <param name="Rank">How well it matched the words; 0 without words.</param>
/// <param name="Name">Its name.</param>
/// <param name="UpdatedAt">When it last changed.</param>
internal sealed record SearchHit(Guid Id, float Rank, string Name, DateTimeOffset UpdatedAt);
