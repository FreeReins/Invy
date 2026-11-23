using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace Invy.Api.Messaging;

/// <summary>Runs the daily backup when its time has come.</summary>
[StickyHandler(MessageQueues.Maintenance)]
public static class RunDueBackupHandler
{
    public static async Task Handle(RunDueBackup command, IMaintenanceManager maintenance, ILogger<RunDueBackup> logger, CancellationToken cancellationToken)
    {
        try
        {
            if (await maintenance.RunDueBackupAsync(command, cancellationToken) is { } backup)
            {
                logger.LogInformation("Backup written: {Name}", backup.Name);
            }
        }
        catch (InvyException exception)
        {
            // Already recorded where an admin can see it; the next check tries again tomorrow.
            logger.LogWarning("The scheduled backup had a problem: {Reason}", exception.Message);
        }
    }
}
