using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="ISettingsAccess"/>
internal sealed class SettingsAccess(InvyDbContext db) : ISettingsAccess
{
    public async Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) =>
        await RowAsync<SecuritySettingsEntity>(cancellationToken) is { } row
            ? new SecuritySettings
            {
                MaxFailedSignIns = row.MaxFailedSignIns,
                LockoutMinutes = row.LockoutMinutes,
                RequireHttps = row.RequireHttps,
                BarcodeLookup = row.BarcodeLookup,
                IsSaved = true,
            }
            : new SecuritySettings();

    public Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new SecuritySettingsEntity
        {
            MaxFailedSignIns = settings.MaxFailedSignIns,
            LockoutMinutes = settings.LockoutMinutes,
            RequireHttps = settings.RequireHttps,
            BarcodeLookup = settings.BarcodeLookup,
        }, cancellationToken);

    public async Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken) =>
        await RowAsync<NotificationSettingsEntity>(cancellationToken) is { } row
            ? new NotificationSettings
            {
                WebhookUrl = row.WebhookUrl,
                RemindersDueAt = row.RemindersDueAt,
                DailyTime = row.DailyTime,
                LastDailyRunOn = row.LastDailyRunOn,
            }
            : new NotificationSettings();

    public Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new NotificationSettingsEntity
        {
            WebhookUrl = settings.WebhookUrl,
            RemindersDueAt = settings.RemindersDueAt,
            DailyTime = settings.DailyTime,
            LastDailyRunOn = settings.LastDailyRunOn,
        }, cancellationToken);

    public async Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken) =>
        await RowAsync<BackupSettingsEntity>(cancellationToken) is { } row
            ? new BackupSettings
            {
                Enabled = row.Enabled,
                Time = row.Time,
                KeepCount = row.KeepCount,
                KeepDays = row.KeepDays,
                LastRunOn = row.LastRunOn,
                LastResult = row.LastResult,
                DueAt = row.DueAt,
            }
            : new BackupSettings();

    public Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new BackupSettingsEntity
        {
            Enabled = settings.Enabled,
            Time = settings.Time,
            KeepCount = settings.KeepCount,
            KeepDays = settings.KeepDays,
            LastRunOn = settings.LastRunOn,
            LastResult = settings.LastResult,
            DueAt = settings.DueAt,
        }, cancellationToken);

    public async Task<AiSettings> GetAiAsync(CancellationToken cancellationToken) => new()
    {
        Models = (await db.AiModels.ToListAsync(cancellationToken))
            .OrderBy(model => model.Position)
            .Select(model => model.ToContract())
            .ToList(),
        Routes = (await db.AiRoutes.ToListAsync(cancellationToken)).Select(route => route.ToContract()).ToList(),
    };

    public async Task SaveAiAsync(AiSettings ai, CancellationToken cancellationToken)
    {
        var tracked = await db.AiModels.ToDictionaryAsync(model => model.Id, cancellationToken);
        var wanted = ai.Models.Select(model => model.ToEntity()).ToList();
        var wantedIds = wanted.Select(model => model.Id).ToHashSet();

        db.AiModels.RemoveRange(tracked.Values.Where(model => !wantedIds.Contains(model.Id)));
        foreach (var fresh in wanted)
        {
            if (tracked.TryGetValue(fresh.Id, out var row))
            {
                db.CopyInto(row, fresh);
            }
            else
            {
                db.AiModels.Add(fresh);
            }
        }

        var trackedRoutes = await db.AiRoutes.ToDictionaryAsync(route => route.Task, cancellationToken);
        var wantedRoutes = ai.Routes.Select(route => route.ToEntity()).ToList();
        var wantedTasks = wantedRoutes.Select(route => route.Task).ToHashSet();
        db.AiRoutes.RemoveRange(trackedRoutes.Values.Where(route => !wantedTasks.Contains(route.Task)));
        foreach (var fresh in wantedRoutes)
        {
            if (trackedRoutes.TryGetValue(fresh.Task, out var row))
            {
                row.ModelId = fresh.ModelId;
            }
            else
            {
                db.AiRoutes.Add(fresh);
            }
        }
    }

    /// <summary>The single row of a settings table, if one has been saved (or added earlier in this request).</summary>
    /// <remarks>Every settings row has Id 1; looking it up by key also finds a row added but not yet saved.</remarks>
    private async Task<TRow?> RowAsync<TRow>(CancellationToken cancellationToken)
        where TRow : class =>
        await db.Set<TRow>().FindAsync([1], cancellationToken);

    private async Task SaveRowAsync<TRow>(TRow fresh, CancellationToken cancellationToken)
        where TRow : class
    {
        if (await RowAsync<TRow>(cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Set<TRow>().Add(fresh);
        }
    }
}
