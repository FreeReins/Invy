namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A time an item spent in one place: inside the same chain of containers.</summary>
/// <param name="From">When it got there, by its own move or one of its containers'.</param>
/// <param name="Until">When it left, or null while it is still there.</param>
/// <param name="Chain">The containers it was in, top first; empty for the top.</param>
public sealed record ItemStay(DateTimeOffset From, DateTimeOffset? Until, IReadOnlyList<ItemRef> Chain);
