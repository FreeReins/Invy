using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ItemEventEntity
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public Guid ItemId { get; set; }

    public DateTimeOffset At { get; set; }

    public Guid? ActorId { get; set; }

    public ItemEventKind Kind { get; set; }

    public Guid? FromParent { get; set; }

    public Guid? ToParent { get; set; }

    public decimal? QtyDelta { get; set; }

    /// <summary>The changes of a Changed event, as JSON in a jsonb column; null for other kinds.</summary>
    public string? Data { get; set; }
}
