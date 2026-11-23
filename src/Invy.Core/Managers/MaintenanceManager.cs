using System.Text.Json;
using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.Utilities;

namespace Invy.Core.Managers;

/// <inheritdoc cref="IMaintenanceManager"/>
internal sealed partial class MaintenanceManager(
    ISettingsAccess settings,
    INotificationAccess webhook,
    IAiAccess ai,
    IBackupAccess backups,
    IMessageAccess messages,
    NotificationEngine notifications,
    ValidationEngine validation,
    TimeProvider clock) : IMaintenanceManager
{
    // ---------- notifications ----------

    public async Task<NotificationSettingsResponse> GetNotificationSettingsAsync(CancellationToken cancellationToken) =>
        (await settings.GetNotificationsAsync(cancellationToken)).ToResponse();

    public async Task<NotificationSettingsResponse> UpdateNotificationSettingsAsync(NotificationSettingsRequest request, CancellationToken cancellationToken)
    {
        var url = ValidationEngine.Webhook(request.WebhookUrl);
        var time = ValidationEngine.TimeOfDay(request.DailyTime);

        var current = await settings.GetNotificationsAsync(cancellationToken);
        current.WebhookUrl = url;
        current.DailyTime = time;
        await ScheduleRemindersAsync(current);
        await settings.SaveNotificationsAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return current.ToResponse();
    }

    public async Task<DeliveryResult> SendTestNotificationAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetNotificationsAsync(cancellationToken);
        if (current.WebhookUrl.Length == 0)
        {
            return new DeliveryResult(false, "Enter a webhook address and save it first.");
        }

        return (await webhook.SendAsync(current.WebhookUrl, notifications.Test(), cancellationToken)).ToResponse();
    }

    // ---------- reminders ----------

    public async Task<ReminderRun> SendTodayAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetNotificationsAsync(cancellationToken);
        var run = await QueueTodayAsync(current, cancellationToken);
        await ScheduleRemindersAsync(current);
        await settings.SaveNotificationsAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return run;
    }

    public async Task<ReminderRun> SendReminderAsync(SendReminder command, CancellationToken cancellationToken)
    {
        // Worded now, so it says what is true when it goes out; dropped if it no longer applies.
        var reminder = notifications.Daily(command.On).FirstOrDefault(reminder => reminder.Type == command.Type);
        return reminder is null ? new ReminderRun([], []) : await DeliverAsync(reminder, cancellationToken);
    }

    public async Task<ReminderRun?> SendDueRemindersAsync(SendDueReminders command, CancellationToken cancellationToken)
    {
        var current = await settings.GetNotificationsAsync(cancellationToken);
        if (command.DueAt is not null && command.DueAt != current.RemindersDueAt)
        {
            return null; // a newer check was scheduled, after a settings change or a restart
        }

        var run = DailyTime.IsDue(current.DailyTime, current.LastDailyRunOn, clock.GetLocalNow())
            ? await QueueTodayAsync(current, cancellationToken)
            : null;
        await ScheduleRemindersAsync(current);
        await settings.SaveNotificationsAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return run;
    }

    // ---------- backups ----------

    public Task<BackupsResponse> GetBackupsAsync(CancellationToken cancellationToken) => DescribeBackupsAsync(cancellationToken);

    public async Task<BackupsResponse> UpdateBackupSettingsAsync(BackupSettingsRequest request, CancellationToken cancellationToken)
    {
        ValidationEngine.BackupLimits(request.KeepCount, request.KeepDays);
        var time = ValidationEngine.TimeOfDay(request.Time);

        var current = await settings.GetBackupsAsync(cancellationToken);
        current.Enabled = request.Enabled;
        current.Time = time;
        current.KeepCount = request.KeepCount;
        current.KeepDays = request.KeepDays;
        await ScheduleBackupAsync(current);
        await settings.SaveBackupsAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await DescribeBackupsAsync(cancellationToken);
    }

    public async Task<BackupsResponse> RunBackupAsync(CancellationToken cancellationToken)
    {
        await BackUpAsync(cancellationToken);
        return await DescribeBackupsAsync(cancellationToken);
    }

    public async Task<BackupFile?> RunDueBackupAsync(RunDueBackup command, CancellationToken cancellationToken)
    {
        var current = await settings.GetBackupsAsync(cancellationToken);
        if (command.DueAt is not null && command.DueAt != current.DueAt)
        {
            return null; // a newer check was scheduled, after a settings change or a restart
        }

        try
        {
            return current.Enabled && DailyTime.IsDue(current.Time, current.LastRunOn, clock.GetLocalNow())
                ? (await BackUpAsync(cancellationToken)).ToResponse()
                : null;
        }
        finally
        {
            // Even after a failed backup, so tomorrow's still happens. Read again: the backup saved its outcome.
            var after = await settings.GetBackupsAsync(cancellationToken);
            await ScheduleBackupAsync(after);
            await settings.SaveBackupsAsync(after, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }
    }

    public Task<Stream> OpenBackupAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(backups.OpenRead(validation.ExistingBackup(name)));

    public async Task<BackupSummary> GetBackupSummaryAsync(string name, CancellationToken cancellationToken)
    {
        var file = validation.ExistingBackup(name);
        try
        {
            return (await backups.CountAsync(file, cancellationToken)).ToResponse();
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException)
        {
            throw new RuleViolationException("This backup couldn't be read. It may be damaged: download it to check, or delete it.");
        }
    }

    public async Task<BackupsResponse> DeleteBackupAsync(string name, CancellationToken cancellationToken)
    {
        backups.Delete(validation.ExistingBackup(name));
        return await DescribeBackupsAsync(cancellationToken);
    }

    public Task RestoreBackupAsync(string name, CancellationToken cancellationToken) =>
        backups.RestoreAsync(validation.ExistingBackup(name), cancellationToken);

    /// <summary>Queues today's wanted reminders and marks today as done, all saved by the caller's one commit.</summary>
    private async Task<ReminderRun> QueueTodayAsync(NotificationSettings current, CancellationToken cancellationToken)
    {
        var today = clock.Today();
        var queued = current.WebhookUrl.Length == 0
            ? []
            : notifications.Daily(today).ToList();
        foreach (var reminder in queued)
        {
            await messages.PublishAsync(new SendReminder(reminder.Type, today));
        }

        // Marked done in the same commit that queues them, so each goes out once, even after a crash.
        current.LastDailyRunOn = today;
        return new ReminderRun(queued.Select(reminder => reminder.Type).ToList(), []);
    }

    /// <summary>
    /// Schedules the next reminder check, replacing any scheduled before: only the one saved as due is acted on.
    /// Saved with <paramref name="current"/> by the caller's commit.
    /// </summary>
    private async Task ScheduleRemindersAsync(NotificationSettings current)
    {
        var next = DailyTime.NextCheck(current.DailyTime, current.LastDailyRunOn, clock.GetUtcNow(), clock.LocalTimeZone);
        current.RemindersDueAt = next;
        await messages.ScheduleAsync(new SendDueReminders(next), next);
    }

    /// <summary>
    /// Schedules the next backup check, replacing any scheduled before; none while backups are off. Saved with
    /// <paramref name="current"/> by the caller's commit.
    /// </summary>
    private async Task ScheduleBackupAsync(BackupSettings current)
    {
        current.DueAt = current.Enabled ? DailyTime.NextCheck(current.Time, current.LastRunOn, clock.GetUtcNow(), clock.LocalTimeZone) : null;
        if (current.DueAt is { } due)
        {
            await messages.ScheduleAsync(new RunDueBackup(due), due);
        }
    }

    /// <summary>
    /// Sends one notification to the webhook, if one is set.
    /// </summary>
    /// <exception cref="TransientDeliveryException">
    /// The webhook couldn't be reached or had a server error, so the message bus tries again later. Other failures,
    /// such as a wrong address, won't get better by waiting, so they are only reported.
    /// </exception>
    private async Task<ReminderRun> DeliverAsync(Notification notification, CancellationToken cancellationToken)
    {
        var current = await settings.GetNotificationsAsync(cancellationToken);
        if (current.WebhookUrl.Length == 0)
        {
            return new ReminderRun([], []);
        }

        var result = await webhook.SendAsync(current.WebhookUrl, notification, cancellationToken);
        if (result.Delivered)
        {
            return new ReminderRun([notification.Type], []);
        }

        return result.Transient
            ? throw new TransientDeliveryException($"{notification.Type}: {result.Problem}")
            : new ReminderRun([], [$"{notification.Type}: {result.Problem}"]);
    }

    /// <summary>Makes a backup now, then deletes the ones that fall outside the retention rules. The outcome is saved either way.</summary>
    private async Task<StoredBackup> BackUpAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetBackupsAsync(cancellationToken);
        var now = clock.GetLocalNow();
        current.LastRunOn = DateOnly.FromDateTime(now.DateTime);

        try
        {
            var file = await backups.CreateAsync(notifications.BackupName(now), cancellationToken);
            var expired = notifications.ExpiredBackups(backups.List(), current, now);
            foreach (var old in expired)
            {
                backups.Delete(old.Name);
            }

            current.LastResult = $"Backed up on {now:yyyy-MM-dd HH:mm}. Removed {expired.Count} old backups.";
            return file;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            current.LastResult = $"The backup on {now:yyyy-MM-dd HH:mm} failed: {exception.Message}";
            throw new RuleViolationException($"The backup couldn't be written: {exception.Message}");
        }
        finally
        {
            await settings.SaveBackupsAsync(current, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }
    }

    private async Task<BackupsResponse> DescribeBackupsAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetBackupsAsync(cancellationToken);
        var files = backups.List().OrderByDescending(file => file.CreatedAt).Select(file => file.ToResponse()).ToList();
        return new BackupsResponse(current.Enabled, current.Time, current.KeepCount, current.KeepDays, current.LastResult, files);
    }
}
