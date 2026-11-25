using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <content>The tables a backup holds, and how each is written, read back and cleared.</content>
internal sealed partial class BackupAccess
{
    /// <summary>
    /// Every table a backup holds, parents before children. Each one is a property of the backup file's JSON,
    /// under the name given, so names never change. A new table is one line here and its
    /// <c>[JsonSerializable(typeof(List&lt;...&gt;))]</c> on <see cref="BackupJson"/>. Restoring clears the
    /// tables in reverse order. Rows reached through <c>Include</c> are written inside their parent.
    /// </summary>
    internal static readonly IReadOnlyList<BackupTable> Tables =
    [
        new BackupTable<UserEntity>("users", BackupJson.Default.ListUserEntity, beforeRestore: EndSessionsAsync),
        new BackupTable<SecuritySettingsEntity>("security", BackupJson.Default.ListSecuritySettingsEntity),
        new BackupTable<NotificationSettingsEntity>("notifications", BackupJson.Default.ListNotificationSettingsEntity),
        new BackupTable<BackupSettingsEntity>("backups", BackupJson.Default.ListBackupSettingsEntity),

        // The AI models, with their API keys, and the tasks sent to a model other than the default.
        new BackupTable<AiModelEntity>("aiModels", BackupJson.Default.ListAiModelEntity),
        new BackupTable<AiRouteEntity>("aiRoutes", BackupJson.Default.ListAiRouteEntity),

        // Each person's inbox, the messages behind the bell.
        new BackupTable<InboxEntryEntity>("inboxEntries", BackupJson.Default.ListInboxEntryEntity),

        // Spaces, with their members inside. Empty in backups made before phase 1.
        new BackupTable<SpaceEntity>("spaces", BackupJson.Default.ListSpaceEntity, query: rows => rows.Include(space => space.Members)),
        new BackupTable<ItemTypeEntity>("itemTypes", BackupJson.Default.ListItemTypeEntity),

        // Items (containers before what is in them), their timelines and sortable values. Empty in backups made
        // before phase 1b.
        new BackupTable<ItemEntity>("items", BackupJson.Default.ListItemEntity, query: rows => rows.OrderBy(item => item.Place.Depth)),
        new BackupTable<ItemEventEntity>("itemEvents", BackupJson.Default.ListItemEventEntity),
        new BackupTable<ItemValueEntity>("itemValues", BackupJson.Default.ListItemValueEntity),

        // QR labels, claimed or not. Empty in backups made before phase 2a.
        new BackupTable<LabelEntity>("labels", BackupJson.Default.ListLabelEntity),

        // What is attached to items. The files themselves are copied beside the backups. Empty in backups made
        // before phase 2b.
        new BackupTable<AttachmentEntity>("attachments", BackupJson.Default.ListAttachmentEntity),

        // Saved views. Empty in backups made before phase 5.
        new BackupTable<ViewEntity>("views", BackupJson.Default.ListViewEntity),

        // Projects, with what each sets aside inside. Empty in backups made before phase 6.
        new BackupTable<ProjectEntity>("projects", BackupJson.Default.ListProjectEntity, query: rows => rows.Include(project => project.Items)),
    ];

    /// <summary>Rows written and read between flushes to the file, and added to the database per save.</summary>
    private const int BatchSize = 1_000;

    /// <summary>
    /// Everyone signs in again after a restore. Passwords go back to the backup's, and so would the session
    /// versions, which would bring back sessions that a password change since then had ended (a stolen cookie,
    /// say). So each account's version moves past both its current one and the backup's, which no cookie
    /// carries. An account removed since the backup has no current version; its sessions ended when it was
    /// removed, unless its password also changed between the backup and the removal.
    /// </summary>
    private static async Task<Action<UserEntity>> EndSessionsAsync(InvyDbContext db, CancellationToken cancellationToken)
    {
        var currentVersions = await db.Users.AsNoTracking().ToDictionaryAsync(user => user.Id, user => user.SessionVersion, cancellationToken);
        return user => user.SessionVersion = Math.Max(user.SessionVersion, currentVersions.GetValueOrDefault(user.Id)) + 1;
    }

    /// <summary>
    /// Items point at other items (their container, their moving destination), and rows are restored a batch at a
    /// time, so these keys are dropped while restoring and added back after, which checks every row in one go. Their
    /// definitions wait in a temporary table that goes with the transaction.
    /// </summary>
    private Task DropItemLinksAsync(CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            """
            CREATE TEMPORARY TABLE invy_item_links ON COMMIT DROP AS
                SELECT conname, pg_get_constraintdef(oid) AS definition FROM pg_constraint
                WHERE contype = 'f' AND conrelid = '"Items"'::regclass AND confrelid = conrelid;
            DO $$
            DECLARE link record;
            BEGIN
                FOR link IN SELECT conname FROM invy_item_links LOOP
                    EXECUTE format('ALTER TABLE "Items" DROP CONSTRAINT %I', link.conname);
                END LOOP;
            END $$;
            """,
            cancellationToken);

    private Task AddItemLinksAsync(CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            DECLARE link record;
            BEGIN
                FOR link IN SELECT conname, definition FROM invy_item_links LOOP
                    EXECUTE format('ALTER TABLE "Items" ADD CONSTRAINT %I %s', link.conname, link.definition);
                END LOOP;
            END $$;
            """,
            cancellationToken);

    /// <summary>One table in a backup file.</summary>
    internal abstract class BackupTable
    {
        /// <summary>The table's property in the backup file's JSON.</summary>
        public abstract string Name { get; }

        /// <summary>The stored entity the table's rows are.</summary>
        public abstract Type EntityType { get; }

        /// <summary>Writes the table's rows as the value of its property, streamed from the database.</summary>
        public abstract Task WriteAsync(InvyDbContext db, Utf8JsonWriter writer, CancellationToken cancellationToken);

        /// <summary>Runs before any table is cleared, while the current rows can still be read.</summary>
        public abstract Task BeforeRestoreAsync(InvyDbContext db, CancellationToken cancellationToken);

        /// <summary>
        /// Reads the value after the table's property (an array of rows; anything else counts as none) and adds the
        /// rows in batches.
        /// </summary>
        public abstract Task RestoreAsync(InvyDbContext db, JsonStreamReader reader, CancellationToken cancellationToken);

        /// <summary>Deletes every row; their children go through the database's foreign keys.</summary>
        public abstract Task ClearAsync(InvyDbContext db, CancellationToken cancellationToken);
    }

    /// <summary>A table of <typeparamref name="TEntity"/> rows.</summary>
    /// <param name="name">The table's property in the backup file's JSON.</param>
    /// <param name="json">How a list of its rows is written, made at build time; its rows are written one by one.</param>
    /// <param name="query">Includes the rows' children, which are written inside them, and orders the rows.</param>
    /// <param name="beforeRestore">Reads what it needs from the current rows, before they are cleared, and adjusts each restored row.</param>
    private sealed class BackupTable<TEntity>(
        string name,
        JsonTypeInfo<List<TEntity>> json,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        Func<InvyDbContext, CancellationToken, Task<Action<TEntity>>>? beforeRestore = null) : BackupTable
        where TEntity : class
    {
        private readonly JsonTypeInfo<TEntity> row = (JsonTypeInfo<TEntity>)json.Options.GetTypeInfo(typeof(TEntity));

        private Action<TEntity>? adjust;

        public override string Name => name;

        public override Type EntityType => typeof(TEntity);

        public override async Task WriteAsync(InvyDbContext db, Utf8JsonWriter writer, CancellationToken cancellationToken)
        {
            var rows = db.Set<TEntity>().AsNoTracking();
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            var written = 0;
            await foreach (var entity in (query?.Invoke(rows) ?? rows).AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                JsonSerializer.Serialize(writer, entity, row);
                if (++written % BatchSize == 0)
                {
                    await writer.FlushAsync(cancellationToken);
                }
            }

            writer.WriteEndArray();
            await writer.FlushAsync(cancellationToken);
        }

        public override async Task BeforeRestoreAsync(InvyDbContext db, CancellationToken cancellationToken) =>
            adjust = beforeRestore is null ? null : await beforeRestore(db, cancellationToken);

        public override async Task RestoreAsync(InvyDbContext db, JsonStreamReader reader, CancellationToken cancellationToken)
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return;
            }

            if (reader.TokenType is JsonTokenType.StartObject)
            {
                throw new InvalidDataException($"The backup file's {name} aren't a list.");
            }

            if (reader.TokenType is not JsonTokenType.StartArray)
            {
                return;
            }

            var batch = new List<TEntity>(BatchSize);
            while (await reader.ReadElementAsync(row, cancellationToken) is { } entity)
            {
                adjust?.Invoke(entity);
                batch.Add(entity);
                if (batch.Count == BatchSize)
                {
                    await SaveAsync(db, batch, cancellationToken);
                }
            }

            await SaveAsync(db, batch, cancellationToken);
        }

        public override Task ClearAsync(InvyDbContext db, CancellationToken cancellationToken) =>
            db.Set<TEntity>().ExecuteDeleteAsync(cancellationToken);

        /// <summary>Adds the batch and forgets it, so the context never holds more than one batch.</summary>
        private static async Task SaveAsync(InvyDbContext db, List<TEntity> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
            {
                return;
            }

            db.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
            batch.Clear();
        }
    }
}
