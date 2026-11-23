namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Where notifications go, and when the daily ones go out.</summary>
/// <param name="WebhookUrl">A URL that accepts a JSON POST, such as a Home Assistant webhook. Empty turns notifications off.</param>
/// <param name="DailyTime">Local time the daily reminders are sent, as HH:mm.</param>
public sealed record NotificationSettingsRequest(string? WebhookUrl, string DailyTime);
