namespace Invy.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class NotificationSettingsEntity
{
    public int Id { get; set; } = 1;

    public string WebhookUrl { get; set; } = string.Empty;

    public DateTimeOffset? RemindersDueAt { get; set; }

    public string DailyTime { get; set; } = string.Empty;

    public DateOnly? LastDailyRunOn { get; set; }
}
