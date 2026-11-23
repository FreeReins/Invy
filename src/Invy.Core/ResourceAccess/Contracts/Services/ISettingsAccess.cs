using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>Settings an admin can change while Invy is running. Defaults are returned when none are saved yet.</summary>
internal interface ISettingsAccess
{
    Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken);

    Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken);

    Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken);

    Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken);

    Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken);

    Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken);

    /// <summary>The AI models in their saved order and the tasks sent to a model other than the default. Empty when none are set up.</summary>
    Task<AiSettings> GetAiAsync(CancellationToken cancellationToken);

    /// <summary>Makes the saved AI models and routes exactly these, models matched by id: others are removed.</summary>
    Task SaveAiAsync(AiSettings ai, CancellationToken cancellationToken);
}
