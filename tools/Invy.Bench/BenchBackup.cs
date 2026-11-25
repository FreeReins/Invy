using System.Diagnostics;
using System.Globalization;
using System.Text;
using Invy.Core.ResourceAccess.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Invy.Bench;

/// <summary>
/// Backs up the seeded database and restores it again through the real backup accessor, sampling the process's
/// memory as it goes: it should stay flat however many rows there are (docs/developers/design.md, Scale targets).
/// </summary>
internal sealed class BenchBackup(IServiceProvider services, string connectionString)
{
    private const string Name = "invy-backup-bench.json.gz";

    public async Task<string> RunAsync(CancellationToken cancellationToken)
    {
        var before = await CountsAsync(cancellationToken);
        Console.WriteLine($"Backing up {before["Items"]:N0} items and {before["ItemEvents"]:N0} events...");
        var (backupTime, backupMemory) = await MeasureAsync(backups => backups.CreateAsync(Name, cancellationToken));
        var size = services.CreateScope().ServiceProvider.GetRequiredService<IBackupAccess>().List().Single(file => file.Name == Name).SizeBytes;

        Console.WriteLine("Restoring it...");
        var (restoreTime, restoreMemory) = await MeasureAsync(backups => backups.RestoreAsync(Name, cancellationToken));
        var after = await CountsAsync(cancellationToken);
        await using (var scope = services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IBackupAccess>().Delete(Name);
        }

        var text = new StringBuilder();
        text.AppendLine("| Backup | Time | Peak memory |");
        text.AppendLine("|---|---:|---:|");
        text.AppendLine(CultureInfo.InvariantCulture, $"| Back up ({size / (1024.0 * 1024):N0} MB file) | {backupTime.TotalMinutes:0.0} min | {backupMemory:N0} MB |");
        text.AppendLine(CultureInfo.InvariantCulture, $"| Restore | {restoreTime.TotalMinutes:0.0} min | {restoreMemory:N0} MB |");
        text.AppendLine();
        text.AppendLine(before.All(pair => after[pair.Key] == pair.Value)
            ? $"Every row came back: {string.Join(", ", after.Select(pair => $"{pair.Value:N0} {pair.Key}"))}."
            : $"Rows are missing after the restore: before {string.Join(", ", before)}, after {string.Join(", ", after)}.");
        return text.ToString();
    }

    /// <summary>How long the call took and the most memory the process held while it ran, in MB, sampled every 100 ms.</summary>
    private async Task<(TimeSpan Elapsed, double PeakMb)> MeasureAsync(Func<IBackupAccess, Task> call)
    {
        GC.Collect();
        var peak = Environment.WorkingSet;
        using var sampling = new CancellationTokenSource();
        var sampler = Task.Run(async () =>
        {
            while (!sampling.IsCancellationRequested)
            {
                peak = Math.Max(peak, Environment.WorkingSet);
                await Task.Delay(100, CancellationToken.None);
            }
        });
        var watch = Stopwatch.StartNew();
        await using (var scope = services.CreateAsyncScope())
        {
            await call(scope.ServiceProvider.GetRequiredService<IBackupAccess>());
        }

        var elapsed = watch.Elapsed;
        await sampling.CancelAsync();
        await sampler;
        return (elapsed, peak / (1024.0 * 1024));
    }

    private async Task<Dictionary<string, long>> CountsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var counts = new Dictionary<string, long>();
        foreach (var table in new[] { "Users", "Spaces", "ItemTypes", "Items", "ItemEvents", "ItemValues" })
        {
            await using var command = new NpgsqlCommand($"SELECT count(*) FROM \"{table}\"", connection);
            counts[table] = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        return counts;
    }
}
