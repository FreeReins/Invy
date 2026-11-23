using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>One entry in an item's timeline. Never changed once written.</summary>
internal sealed class ItemEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SpaceId { get; set; }

    public Guid ItemId { get; set; }

    public DateTimeOffset At { get; set; }

    /// <summary>The account that made the change, or null when Invy did it.</summary>
    public Guid? ActorId { get; set; }

    public ItemEventKind Kind { get; set; }

    /// <summary>The container it came out of, for Created and Moved. Null at the top.</summary>
    public Guid? FromParent { get; set; }

    /// <summary>The container it went into, for Created and Moved. Null at the top.</summary>
    public Guid? ToParent { get; set; }

    /// <summary>How much the amount went up (or, negative, down), for QuantityChanged.</summary>
    public decimal? QtyDelta { get; set; }

    /// <summary>What changed, for Changed.</summary>
    public List<ItemChange> Changes { get; set; } = [];
}
