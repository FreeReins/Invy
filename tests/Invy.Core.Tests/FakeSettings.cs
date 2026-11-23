using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

internal sealed class FakeSettings : ISettingsAccess
{
    public SecuritySettings Security { get; set; } = new();

    public NotificationSettings Notifications { get; set; } = new();

    public BackupSettings Backups { get; set; } = new();

    public AiSettings Ai { get; set; } = new();

    public Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Security));

    public Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) =>
        Set(() =>
        {
            Security = Copies.Of(settings);
            Security.IsSaved = true;
        });

    public Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Notifications));

    public Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken) => Set(() => Notifications = Copies.Of(settings));

    public Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Backups));

    public Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken) => Set(() => Backups = Copies.Of(settings));

    public Task<AiSettings> GetAiAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Ai));

    public Task SaveAiAsync(AiSettings ai, CancellationToken cancellationToken) => Set(() => Ai = Copies.Of(ai));

    private static Task Set(Action save)
    {
        save();
        return Task.CompletedTask;
    }
}
