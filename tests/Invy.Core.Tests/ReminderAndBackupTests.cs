using System.IO.Compression;
using System.Text;
using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Tests;

public class ReminderAndBackupTests
{
    [Test]
    public async Task Daily_jobs_run_once_after_their_time()
    {
        var offset = TimeSpan.FromHours(-6);
        var morning = new DateTimeOffset(2026, 11, 10, 7, 0, 0, offset);
        var later = new DateTimeOffset(2026, 11, 10, 7, 45, 0, offset);
        var today = new DateOnly(2026, 11, 10);

        await Assert.That(DailyTime.IsDue("07:30", null, morning)).IsFalse();
        await Assert.That(DailyTime.IsDue("07:30", today.AddDays(-1), later)).IsTrue();
        await Assert.That(DailyTime.IsDue("07:30", today, later)).IsFalse();
    }

    [Test]
    public async Task Times_must_be_hours_and_minutes()
    {
        var refused = false;
        try
        {
            DailyTime.Parse("half past seven");
        }
        catch (RuleViolationException)
        {
            refused = true;
        }

        await Assert.That(refused).IsTrue();
        await Assert.That(DailyTime.Format(DailyTime.Parse("07:05"))).IsEqualTo("07:05");
    }

    [Test]
    public async Task Retention_keeps_the_newest_backups()
    {
        var now = new DateTimeOffset(2026, 11, 20, 3, 0, 0, TimeSpan.Zero);
        var files = Enumerable.Range(0, 5).Select(age => new StoredBackup($"backup-{age}", now.AddDays(-age), 100)).ToList();

        var expired = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 3, KeepDays = 0 }, now);

        await Assert.That(string.Join(",", expired.Select(file => file.Name))).IsEqualTo("backup-3,backup-4");
    }

    [Test]
    public async Task Retention_removes_backups_past_their_age_but_never_the_newest()
    {
        var now = new DateTimeOffset(2026, 11, 20, 3, 0, 0, TimeSpan.Zero);
        var files = new[] { 10, 20, 40 }.Select(age => new StoredBackup($"backup-{age}", now.AddDays(-age), 100)).ToList();

        var byAge = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 0, KeepDays = 15 }, now);
        var keepAll = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 0, KeepDays = 0 }, now);
        var allTooOld = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 0, KeepDays = 5 }, now);

        await Assert.That(string.Join(",", byAge.Select(file => file.Name))).IsEqualTo("backup-20,backup-40");
        await Assert.That(keepAll.Count).IsEqualTo(0);
        await Assert.That(string.Join(",", allTooOld.Select(file => file.Name))).IsEqualTo("backup-20,backup-40");
    }

    [Test]
    public async Task Counting_a_backup_reads_what_it_holds_and_treats_missing_lists_as_empty()
    {
        var folder = Directory.CreateTempSubdirectory("invy-backup-test").FullName;
        try
        {
            // A list the backup lacks (inboxEntries here) reads as empty, and lists Invy doesn't know are skipped.
            const string name = "invy-backup-test.json.gz";
            const string json = """
                {
                  "createdAt": "2026-01-01T00:00:00Z",
                  "somethingNewer": [{ "id": 1 }],
                  "users": [{ "id": 1, "username": "admin" }, { "id": 2 }]
                }
                """;
            await using (var file = File.Create(Path.Combine(folder, name)))
            await using (var zip = new GZipStream(file, CompressionLevel.Fastest))
            {
                await zip.WriteAsync(Encoding.UTF8.GetBytes(json));
            }

            var counts = await new BackupAccess(null!, folder, folder).CountAsync(name, CancellationToken.None);

            await Assert.That(counts).IsEqualTo(new BackupCounts(2));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
