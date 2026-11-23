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
    ];

    /// <summary>
    /// Everyone signs in again after a restore. Passwords go back to the backup's, and so would the session
    /// versions, which would bring back sessions that a password change since then had ended (a stolen cookie,
    /// say). So each account's version moves past both its current one and the backup's, which no cookie
    /// carries. An account removed since the backup has no current version; its sessions ended when it was
    /// removed, unless its password also changed between the backup and the removal.
    /// </summary>
    private static async Task EndSessionsAsync(InvyDbContext db, List<UserEntity> users, CancellationToken cancellationToken)
    {
        var currentVersions = await db.Users.AsNoTracking().ToDictionaryAsync(user => user.Id, user => user.SessionVersion, cancellationToken);
        foreach (var user in users)
        {
            user.SessionVersion = Math.Max(user.SessionVersion, currentVersions.GetValueOrDefault(user.Id)) + 1;
        }
    }

    /// <summary>One table in a backup file.</summary>
    internal abstract class BackupTable
    {
        /// <summary>The table's property in the backup file's JSON.</summary>
        public abstract string Name { get; }

        /// <summary>The stored entity the table's rows are.</summary>
        public abstract Type EntityType { get; }

        /// <summary>Writes the table's rows as the value of its property.</summary>
        public abstract Task WriteAsync(InvyDbContext db, Utf8JsonWriter writer, CancellationToken cancellationToken);

        /// <summary>Reads the table's rows from a backup file; none when an older backup lacks the table.</summary>
        public abstract BackupRows Read(JsonElement backup);

        /// <summary>Deletes every row; their children go through the database's foreign keys.</summary>
        public abstract Task ClearAsync(InvyDbContext db, CancellationToken cancellationToken);
    }

    /// <summary>A table's rows read from a backup file, waiting to be restored.</summary>
    internal abstract class BackupRows
    {
        /// <summary>Runs before any table is cleared, while the current rows can still be read.</summary>
        public abstract Task BeforeRestoreAsync(InvyDbContext db, CancellationToken cancellationToken);

        /// <summary>Adds the rows to the context, to be inserted on save.</summary>
        public abstract void AddTo(InvyDbContext db);
    }

    /// <summary>A table of <typeparamref name="TEntity"/> rows.</summary>
    /// <param name="name">The table's property in the backup file's JSON.</param>
    /// <param name="json">How a list of its rows is written, made at build time.</param>
    /// <param name="query">Includes the rows' children, which are written inside them.</param>
    /// <param name="prepare">Tidies rows just read from the database or from a backup file.</param>
    /// <param name="beforeRestore">Adjusts the backup's rows from the current ones, before they are cleared.</param>
    private sealed class BackupTable<TEntity>(
        string name,
        JsonTypeInfo<List<TEntity>> json,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        Action<List<TEntity>>? prepare = null,
        Func<InvyDbContext, List<TEntity>, CancellationToken, Task>? beforeRestore = null) : BackupTable
        where TEntity : class
    {
        public override string Name => name;

        public override Type EntityType => typeof(TEntity);

        public override async Task WriteAsync(InvyDbContext db, Utf8JsonWriter writer, CancellationToken cancellationToken)
        {
            var rows = db.Set<TEntity>().AsNoTracking();
            var list = await (query?.Invoke(rows) ?? rows).ToListAsync(cancellationToken);
            prepare?.Invoke(list);
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, list, json);
        }

        public override BackupRows Read(JsonElement backup)
        {
            var list = backup.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
                ? value.Deserialize(json) ?? []
                : [];
            prepare?.Invoke(list);
            return new Rows(list, beforeRestore);
        }

        public override Task ClearAsync(InvyDbContext db, CancellationToken cancellationToken) =>
            db.Set<TEntity>().ExecuteDeleteAsync(cancellationToken);

        private sealed class Rows(List<TEntity> list, Func<InvyDbContext, List<TEntity>, CancellationToken, Task>? beforeRestore) : BackupRows
        {
            public override Task BeforeRestoreAsync(InvyDbContext db, CancellationToken cancellationToken) =>
                beforeRestore?.Invoke(db, list, cancellationToken) ?? Task.CompletedTask;

            public override void AddTo(InvyDbContext db) => db.AddRange(list);
        }
    }
}
