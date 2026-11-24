namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>The best matches for what someone is looking for, each with where it is.</summary>
/// <param name="Matches">Up to 10 items, best match first. Empty when nothing matches.</param>
/// <param name="Total">How many items match in all; more than <see cref="Matches"/> holds when the words are vague.</param>
public sealed record WhereIsResponse(IReadOnlyList<ItemLocation> Matches, int Total);
