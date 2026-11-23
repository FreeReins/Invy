namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One entry in a timeline.</summary>
/// <param name="Id">The event's id.</param>
/// <param name="Kind">What happened.</param>
/// <param name="At">When.</param>
/// <param name="Item">The item it happened to.</param>
/// <param name="Actor">Who did it, by name, or null when Invy did.</param>
/// <param name="From">For Created and Moved: the container it came out of, or null for the top.</param>
/// <param name="To">For Created and Moved: the container it went into, or null for the top.</param>
/// <param name="QtyDelta">For QuantityChanged: how much the amount went up, or (negative) down.</param>
/// <param name="Changes">For Changed: what changed.</param>
public sealed record ItemEventResponse(
    Guid Id,
    ItemEventKind Kind,
    DateTimeOffset At,
    ItemRef Item,
    string? Actor,
    ItemRef? From,
    ItemRef? To,
    decimal? QtyDelta,
    IReadOnlyList<ItemChange> Changes);
