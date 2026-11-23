using System.Globalization;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>
/// Works out what to tell people and when: the daily reminders, and which old backups to delete. It is given the
/// settings and returns decisions, so every rule can be tested without a clock or a network. Reads and saves nothing.
/// </summary>
internal sealed class NotificationEngine
{
    /// <summary>
    /// The reminders for one day, in the order they should arrive. None yet: reminders for warranties, upkeep, lent
    /// items and low stock come with the features they are about.
    /// </summary>
    /// <param name="today">The day the reminders are for.</param>
    public IReadOnlyList<Notification> Daily(DateOnly today) => [];

    /// <summary>The sample sent from the Settings tab.</summary>
    public Notification Test() => new(NotificationTypes.Test, "Invy test", "Notifications from Invy are working.",
        new Dictionary<string, object?> { ["sentBy"] = "Settings" });

    /// <summary>The backups that fall outside the retention rules. The newest backup is always kept.</summary>
    public IReadOnlyList<StoredBackup> ExpiredBackups(IEnumerable<StoredBackup> files, BackupSettings settings, DateTimeOffset now)
    {
        var newestFirst = files.OrderByDescending(file => file.CreatedAt).ToList();
        return newestFirst
            .Where((file, index) => index > 0 && (
                (settings.KeepCount > 0 && index >= settings.KeepCount) ||
                (settings.KeepDays > 0 && file.CreatedAt < now.AddDays(-settings.KeepDays))))
            .ToList();
    }

    /// <summary>The file name for a backup made at <paramref name="localNow"/>.</summary>
    public string BackupName(DateTimeOffset localNow) =>
        $"invy-backup-{localNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.json.gz";
}
