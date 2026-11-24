namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Reminder dates coming up, and upkeep that is overdue, soonest first.</summary>
/// <param name="Dates">The dates.</param>
/// <param name="More">True when there were more than the list holds.</param>
public sealed record RemindersResponse(IReadOnlyList<ReminderDate> Dates, bool More);
