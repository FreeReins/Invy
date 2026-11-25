using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace Invy.Seed;

/// <summary>
/// Streams items, their paths, events and sortable values into four binary COPY statements at once, one connection
/// each, so PostgreSQL loads the four tables in parallel and nothing is held in memory. Foreign keys aren't checked while
/// copying (<c>session_replication_role = replica</c>): every row is made consistent here.
/// </summary>
internal sealed class SeedWriter : IAsyncDisposable
{
    private readonly List<NpgsqlConnection> connections = [];
    private NpgsqlBinaryImporter items = null!;
    private NpgsqlBinaryImporter paths = null!;
    private NpgsqlBinaryImporter events = null!;
    private NpgsqlBinaryImporter values = null!;

    public long Items { get; private set; }

    public long Events { get; private set; }

    public long Values { get; private set; }

    public static async Task<SeedWriter> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var writer = new SeedWriter();
        writer.items = await writer.CopyAsync(
            connectionString,
            """
            COPY "Items" ("Id", "SpaceId", "TypeId", "Name", "Marking", "ParentId", "HoldsItems", "Quantity", "Unit",
                "LentTo", "LentAt", "Fields", "Tags", "Notes", "Status", "LastSeenAt", "CreatedAt", "UpdatedAt", "Value", "ContentCount",
                "ContentValue", "Reserved", "IsExample", "Version") FROM STDIN (FORMAT BINARY)
            """,
            cancellationToken);
        writer.paths = await writer.CopyAsync(
            connectionString, """COPY "ItemPaths" ("Id", "Path", "Depth") FROM STDIN (FORMAT BINARY)""", cancellationToken);
        writer.events = await writer.CopyAsync(
            connectionString,
            """
            COPY "ItemEvents" ("Id", "SpaceId", "ItemId", "At", "ActorId", "Kind", "FromParent", "ToParent", "QtyDelta", "Data")
            FROM STDIN (FORMAT BINARY)
            """,
            cancellationToken);
        writer.values = await writer.CopyAsync(
            connectionString,
            """COPY "ItemValues" ("SpaceId", "ItemId", "FieldId", "Num", "Date", "Text") FROM STDIN (FORMAT BINARY)""",
            cancellationToken);
        return writer;
    }

    public void Write(Item item, IReadOnlyList<ItemValue> itemValues, IReadOnlyList<ItemEvent> itemEvents)
    {
        var entity = item.ToEntity();
        items.StartRow();
        items.Write(entity.Id, NpgsqlDbType.Uuid);
        items.Write(entity.SpaceId, NpgsqlDbType.Uuid);
        Write(items, entity.TypeId, NpgsqlDbType.Uuid);
        items.Write(entity.Name, NpgsqlDbType.Text);
        Write(items, entity.Marking, NpgsqlDbType.Text);
        Write(items, entity.ParentId, NpgsqlDbType.Uuid);
        items.Write(entity.HoldsItems, NpgsqlDbType.Boolean);
        Write(items, entity.Quantity, NpgsqlDbType.Numeric);
        Write(items, entity.Unit, NpgsqlDbType.Text);
        Write(items, entity.LentTo, NpgsqlDbType.Text);
        Write(items, entity.LentAt?.ToUniversalTime(), NpgsqlDbType.TimestampTz);
        items.Write(entity.Fields, NpgsqlDbType.Jsonb);
        items.Write(entity.Tags.ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text);
        Write(items, entity.Notes, NpgsqlDbType.Text);
        items.Write(entity.Status.ToString(), NpgsqlDbType.Varchar);
        Write(items, entity.LastSeenAt?.ToUniversalTime(), NpgsqlDbType.TimestampTz);
        items.Write(entity.CreatedAt.ToUniversalTime(), NpgsqlDbType.TimestampTz);
        items.Write(entity.UpdatedAt.ToUniversalTime(), NpgsqlDbType.TimestampTz);
        Write(items, entity.Value, NpgsqlDbType.Numeric);
        items.Write(entity.ContentCount, NpgsqlDbType.Integer);
        items.Write(entity.ContentValue, NpgsqlDbType.Numeric);
        items.Write(entity.Reserved, NpgsqlDbType.Numeric);
        items.Write(entity.IsExample, NpgsqlDbType.Boolean);
        items.Write(entity.Version, NpgsqlDbType.Integer);
        Items++;

        paths.StartRow();
        paths.Write(entity.Id, NpgsqlDbType.Uuid);
        paths.Write(entity.Path, NpgsqlDbType.LTree);
        paths.Write(entity.Depth, NpgsqlDbType.Integer);

        foreach (var itemEvent in itemEvents)
        {
            var row = itemEvent.ToEntity();
            events.StartRow();
            events.Write(row.Id, NpgsqlDbType.Uuid);
            events.Write(row.SpaceId, NpgsqlDbType.Uuid);
            events.Write(row.ItemId, NpgsqlDbType.Uuid);
            events.Write(row.At.ToUniversalTime(), NpgsqlDbType.TimestampTz);
            Write(events, row.ActorId, NpgsqlDbType.Uuid);
            events.Write(row.Kind.ToString(), NpgsqlDbType.Varchar);
            Write(events, row.FromParent, NpgsqlDbType.Uuid);
            Write(events, row.ToParent, NpgsqlDbType.Uuid);
            Write(events, row.QtyDelta, NpgsqlDbType.Numeric);
            Write(events, row.Data, NpgsqlDbType.Jsonb);
        }

        Events += itemEvents.Count;

        foreach (var value in itemValues)
        {
            values.StartRow();
            values.Write(value.SpaceId, NpgsqlDbType.Uuid);
            values.Write(value.ItemId, NpgsqlDbType.Uuid);
            values.Write(value.FieldId, NpgsqlDbType.Uuid);
            Write(values, value.Num, NpgsqlDbType.Numeric);
            Write(values, value.Date, NpgsqlDbType.Date);
            Write(values, value.Text, NpgsqlDbType.Text);
        }

        Values += itemValues.Count;
    }

    /// <summary>Finishes the four COPY statements: nothing is kept unless this runs.</summary>
    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        await items.CompleteAsync(cancellationToken);
        await paths.CompleteAsync(cancellationToken);
        await events.CompleteAsync(cancellationToken);
        await values.CompleteAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var importer in new[] { items, paths, events, values }.Where(importer => importer is not null))
        {
            await importer.DisposeAsync();
        }

        foreach (var connection in connections)
        {
            await connection.DisposeAsync();
        }
    }

    private static void Write<T>(NpgsqlBinaryImporter copy, T? value, NpgsqlDbType type)
        where T : struct
    {
        if (value is T present)
        {
            copy.Write(present, type);
        }
        else
        {
            copy.WriteNull();
        }
    }

    private static void Write(NpgsqlBinaryImporter copy, string? value, NpgsqlDbType type)
    {
        if (value is null)
        {
            copy.WriteNull();
        }
        else
        {
            copy.Write(value, type);
        }
    }

    private async Task<NpgsqlBinaryImporter> CopyAsync(string connectionString, string copy, CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        connections.Add(connection);
        await connection.OpenAsync(cancellationToken);
        await using (var command = new NpgsqlCommand("SET session_replication_role = replica", connection))
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return await connection.BeginBinaryImportAsync(copy, cancellationToken);
    }
}
