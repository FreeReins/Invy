using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <summary>
/// Backs the database up to compressed JSON files and restores from them. The files hold
/// Invy's own data rather than a PostgreSQL dump, so they don't depend on database tools
/// or versions. They include password hashes: treat them as private. The message bus's own
/// tables are not included.
/// <para>
/// Attached files are copied into the backup folder's <c>files</c> folder as each backup is made. They are named by
/// their SHA-256 and never change, so each one is copied once and shared by every backup that needs it; restoring
/// puts back any that are missing. Removing old backups leaves these copies.
/// </para>
/// </summary>
/// <param name="db">The database.</param>
/// <param name="folder">The backup folder (Data:Backups).</param>
/// <param name="filesFolder">Attached files (Data:Files).</param>
internal sealed partial class BackupAccess(InvyDbContext db, string folder, string filesFolder) : IBackupAccess
{
    private const string Pattern = "invy-backup-*.json.gz";

    /// <summary>Where attached files are copied, inside the backup folder.</summary>
    private string FileCopies => Path.Combine(folder, "files");

    public async Task<StoredBackup> CreateAsync(string name, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        await using (var file = File.Create(path))
        await using (var zip = new GZipStream(file, CompressionLevel.Optimal))
        await using (var writer = new Utf8JsonWriter(zip))
        {
            writer.WriteStartObject();
            writer.WriteString("createdAt", DateTimeOffset.UtcNow);
            foreach (var table in Tables)
            {
                // One table's rows in memory at a time.
                await table.WriteAsync(db, writer, cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }

            writer.WriteEndObject();
        }

        CopyMissing(filesFolder, FileCopies);
        return Describe(new FileInfo(path));
    }

    public IReadOnlyList<StoredBackup> List() =>
        Directory.Exists(folder)
            ? new DirectoryInfo(folder).GetFiles(Pattern).Select(Describe).ToList()
            : [];

    public void Delete(string name) => File.Delete(Path.Combine(folder, name));

    public Stream OpenRead(string name) => File.OpenRead(Path.Combine(folder, name));

    public async Task<BackupCounts> CountAsync(string name, CancellationToken cancellationToken)
    {
        BackupOutline outline;
        await using (var file = File.OpenRead(Path.Combine(folder, name)))
        await using (var zip = new GZipStream(file, CompressionMode.Decompress))
        {
            outline = await JsonSerializer.DeserializeAsync(zip, BackupJson.Default.BackupOutline, cancellationToken)
                ?? throw new InvalidDataException("The backup file is empty.");
        }

        return new BackupCounts(outline.Users.Count);
    }

    public async Task RestoreAsync(string name, CancellationToken cancellationToken)
    {
        List<BackupRows> restored;
        await using (var file = File.OpenRead(Path.Combine(folder, name)))
        await using (var zip = new GZipStream(file, CompressionMode.Decompress))
        {
            using var backup = await JsonDocument.ParseAsync(zip, cancellationToken: cancellationToken);
            if (backup.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("The backup file is empty.");
            }

            restored = Tables.Select(table => table.Read(backup.RootElement)).ToList();
        }

        // One transaction: either everything is replaced, or nothing changes.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var rows in restored)
        {
            await rows.BeforeRestoreAsync(db, cancellationToken);
        }

        // These aren't backed up, and point at users. Access links are made again. API tokens aren't backed up, so
        // a token revoked since the backup can't come back: like everyone signing in again, assistants need new
        // tokens after a restore.
        await db.AccessLinks.ExecuteDeleteAsync(cancellationToken);
        await db.ApiTokens.ExecuteDeleteAsync(cancellationToken);
        foreach (var table in Tables.Reverse())
        {
            await table.ClearAsync(db, cancellationToken);
        }

        db.ChangeTracker.Clear();
        foreach (var rows in restored)
        {
            rows.AddTo(db);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
        CopyMissing(FileCopies, filesFolder);
    }

    /// <summary>
    /// Copies every file in <paramref name="from"/> (and its folders) that <paramref name="to"/> lacks. Files are named
    /// by their contents, so one with the same name is the same file. Half-written ones (.partial) are skipped.
    /// </summary>
    private static void CopyMissing(string from, string to)
    {
        if (!Directory.Exists(from))
        {
            return;
        }

        foreach (var source in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            if (source.EndsWith(".partial", StringComparison.Ordinal))
            {
                continue;
            }

            var target = Path.Combine(to, Path.GetRelativePath(from, source));
            if (File.Exists(target))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            var partial = $"{target}.{Guid.NewGuid():N}.partial";
            File.Copy(source, partial);
            File.Move(partial, target, overwrite: true);
        }
    }

    private static StoredBackup Describe(FileInfo file) =>
        new(file.Name, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero), file.Length);

    /// <summary>
    /// Just enough of a backup file to count what it holds. The records' fields are skipped, so
    /// no entities are built. A list an older backup lacks reads as empty.
    /// </summary>
    private sealed class BackupOutline
    {
        public List<Counted> Users { get; init => field = value ?? []; } = [];
    }

    /// <summary>One record in a backup, counted but not read.</summary>
    private sealed class Counted;

    /// <summary>The backup file's JSON shape, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<UserEntity>))]
    [JsonSerializable(typeof(List<SecuritySettingsEntity>))]
    [JsonSerializable(typeof(List<NotificationSettingsEntity>))]
    [JsonSerializable(typeof(List<BackupSettingsEntity>))]
    [JsonSerializable(typeof(List<AiModelEntity>))]
    [JsonSerializable(typeof(List<AiRouteEntity>))]
    [JsonSerializable(typeof(List<InboxEntryEntity>))]
    [JsonSerializable(typeof(List<SpaceEntity>))]
    [JsonSerializable(typeof(List<ItemTypeEntity>))]
    [JsonSerializable(typeof(List<ItemEntity>))]
    [JsonSerializable(typeof(List<ItemEventEntity>))]
    [JsonSerializable(typeof(List<ItemValueEntity>))]
    [JsonSerializable(typeof(List<LabelEntity>))]
    [JsonSerializable(typeof(List<AttachmentEntity>))]
    [JsonSerializable(typeof(List<ViewEntity>))]
    [JsonSerializable(typeof(BackupOutline))]
    private sealed partial class BackupJson : JsonSerializerContext;
}
