using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <summary>Turns the internal data contracts and engine results into the public response messages, and back.</summary>
internal static partial class Mapping
{
    public static UserResponse ToResponse(this UserAccount user) =>
        new(user.Id, user.Username, user.DisplayName, user.IsAdmin, user.CreatedAt, user.LockedUntil);

    public static SessionInfo ToSession(this UserAccount user) => new(user.Id, user.Username, user.IsAdmin, user.SessionVersion);

    public static AccountSession ToAccountSession(this UserAccount user) => new(user.ToResponse(), user.ToSession());

    public static SecurityResponse ToResponse(this SecuritySettings rules) =>
        new(rules.MaxFailedSignIns, rules.LockoutMinutes, rules.RequireHttps, rules.BarcodeLookup);

    public static NotificationSettingsResponse ToResponse(this NotificationSettings settings) =>
        new(settings.WebhookUrl, settings.DailyTime, settings.LastDailyRunOn);

    public static BackupFile ToResponse(this StoredBackup file) => new(file.Name, file.CreatedAt, file.SizeBytes);

    public static BackupSummary ToResponse(this BackupCounts counts) => new(counts.Users);

    public static DeliveryResult ToResponse(this Delivery delivery) => new(delivery.Delivered, delivery.Problem);
}
