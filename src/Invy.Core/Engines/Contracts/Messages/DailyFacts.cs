namespace Invy.Core.Engines.Contracts.Messages;

/// <summary>
/// What the daily reminders are worked out from, gathered from the spaces whose feature is on. The lists may hold
/// more than is due today: <see cref="NotificationEngine"/> picks what to say.
/// </summary>
/// <param name="Low">Counted items at or below their low-stock level, in spaces with Stock on.</param>
/// <param name="Lent">Items lent out with a due date, in spaces with Lending on.</param>
/// <param name="Dates">Reminder dates near today, in spaces with Reminders on.</param>
internal sealed record DailyFacts(IReadOnlyList<SpaceItem> Low, IReadOnlyList<SpaceItem> Lent, IReadOnlyList<DueDate> Dates)
{
    /// <summary>Nothing to remind anyone of.</summary>
    public static DailyFacts None { get; } = new([], [], []);
}
