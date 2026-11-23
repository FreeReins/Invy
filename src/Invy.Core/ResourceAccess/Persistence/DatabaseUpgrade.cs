using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Invy.Core.ResourceAccess.Persistence;

/// <summary>Brings the database up to date with the migrations in <c>Persistence/Migrations</c>.</summary>
internal static class DatabaseUpgrade
{
    /// <summary>The first migration: the schema Invy started with. Never edit or rename it.</summary>
    public const string BaselineMigration = "20261123035820_Baseline";

    public static async Task RunAsync(InvyDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Updating the database: {Migrations}.", string.Join(", ", pending));
        }

        await context.Database.MigrateAsync(cancellationToken);
    }
}
