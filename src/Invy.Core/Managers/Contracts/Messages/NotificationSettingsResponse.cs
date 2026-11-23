namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>The notification settings, and the last day the daily reminders went out.</summary>
/// <param name="WebhookUrl">Where notifications are posted. Empty when they are off.</param>
/// <param name="DailyTime">Local time the daily reminders are sent, as HH:mm.</param>
/// <param name="LastDailyRunOn">The last day the daily reminders ran, if they have.</param>
public sealed record NotificationSettingsResponse(string WebhookUrl, string DailyTime, DateOnly? LastDailyRunOn);
