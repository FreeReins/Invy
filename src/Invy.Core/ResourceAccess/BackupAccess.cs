using System.Data;
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

    /// <summary>How a backup file is created: readable only by Invy's own user, outside Windows.</summary>
    private static FileStreamOptions PrivateFile
    {
        get
        {
            var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            return options;
        }
    }

    /// <summary>Where attached files are copied, inside the backup folder.</summary>
    private string FileCopies => Path.Combine(folder, "files");

    public async Task<StoredBackup> CreateAsync(string name, CancellationToken cancellationToken)
    {
        // A backup holds every account's password hash and everything in every space, so on Linux and macOS only
        // Invy's own user can read it. On Windows the folder's permissions decide, as for the rest of Data:Root.
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(folder);
        }
        else
        {
            Directory.CreateDirectory(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var path = Path.Combine(folder, name);
        await using (var file = new FileStream(path, PrivateFile))
        await using (var zip = new GZipStream(file, CompressionLevel.Optimal))
        await using (var writer = new Utf8JsonWriter(zip))
        {
            // One snapshot for every table, so rows added while the backup runs can't point at rows it left out. Rows
            // are streamed from the database to the file: only a few at a time are in memory, however many there are.
            await using var snapshot = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
            writer.WriteStartObject();
            writer.WriteString("createdAt", DateTimeOffset.UtcNow);
            foreach (var table in Tables)
            {
                await table.WriteAsync(db, writer, cancellationToken);
            }

            writer.WriteEndObject();
            await writer.FlushAsync(cancellationToken);
            await snapshot.CommitAsync(cancellationToken);
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
        // Only the accounts are counted. They come first, so the rest of the file is never read.
        await using var file = File.OpenRead(Path.Combine(folder, name));
        await using var zip = new GZipStream(file, CompressionMode.Decompress);
        var reader = new JsonStreamReader(zip);
        if (!await reader.ReadAsync(cancellationToken) || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidDataException("The backup file is empty.");
        }

        while (await reader.ReadAsync(cancellationToken) && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.Text != "users")
            {
                await reader.SkipValueAsync(cancellationToken);
                continue;
            }

            var users = 0;
            if (await reader.ReadAsync(cancellationToken) && reader.TokenType == JsonTokenType.StartArray)
            {
                while (await reader.ReadElementAsync(BackupJson.Default.Counted, cancellationToken) is not null)
                {
                    users++;
                }
            }

            return new BackupCounts(users);
        }

        return new BackupCounts(0);
    }

    public async Task RestoreAsync(string name, CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(Path.Combine(folder, name));
        await using var zip = new GZipStream(file, CompressionMode.Decompress);
        var reader = new JsonStreamReader(zip);
        if (!await reader.ReadAsync(cancellationToken) || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidDataException("The backup file is empty.");
        }

        // One transaction: either everything is replaced, or nothing changes, even when the file turns out to be
        // damaged part way through. The rows are streamed from the file in batches, never all read at once.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var table in Tables)
        {
            await table.BeforeRestoreAsync(db, cancellationToken);
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
        await DropItemLinksAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken) && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (Tables.FirstOrDefault(table => table.Name == reader.Text) is { } table)
            {
                await table.RestoreAsync(db, reader, cancellationToken);
            }
            else
            {
                await reader.SkipValueAsync(cancellationToken);
            }
        }

        await AddItemLinksAsync(cancellationToken);
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
    [JsonSerializable(typeof(List<ProjectEntity>))]
    [JsonSerializable(typeof(Counted))]
    private sealed partial class BackupJson : JsonSerializerContext;
}
