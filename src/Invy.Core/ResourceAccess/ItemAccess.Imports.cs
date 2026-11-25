using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Invy.Core.ResourceAccess;

/// <content>Bulk inserts for imports, through PostgreSQL's COPY, and finding an import's location containers.</content>
internal sealed partial class ItemAccess
{
    public async Task<Item?> FindContainerAsync(Guid spaceId, Guid? parentId, string name, CancellationToken cancellationToken)
    {
        var lower = name.ToLower();
        var found = db.Items.Local.FirstOrDefault(item => item.SpaceId == spaceId && item.ParentId == parentId && item.HoldsItems
                && string.Equals(item.Name, name, StringComparison.CurrentCultureIgnoreCase))
            ?? await Current(spaceId)
                .Where(item => item.ParentId == parentId && item.HoldsItems && item.Name.ToLower() == lower)
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
        return found?.ToContract();
    }

    public async Task InsertManyAsync(
        IReadOnlyList<Item> items, IReadOnlyList<ItemEvent> events, IReadOnlyList<ItemValue> values, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        // COPY runs on the request's connection, inside its transaction, so the batch commits with everything else.
        if (db.Database.CurrentTransaction is null)
        {
            await db.Database.BeginTransactionAsync(cancellationToken);
        }

        // Rows added through the context earlier in the batch (the location containers) are written first, still in
        // the transaction, so the copied items' containers exist when COPY checks them.
        await db.SaveChangesAsync(cancellationToken);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using (var copy = await connection.BeginBinaryImportAsync(
            """
            COPY "Items" ("Id", "SpaceId", "TypeId", "Name", "Marking", "ParentId", "HoldsItems", "Quantity", "Unit",
                "Fields", "Tags", "Notes", "Status", "CreatedAt", "UpdatedAt", "Value", "ContentCount", "ContentValue", "Reserved",
                "IsExample", "Version") FROM STDIN (FORMAT BINARY)
            """, cancellationToken))
        {
            foreach (var entity in items.Select(item => item.ToEntity()))
            {
                await copy.StartRowAsync(cancellationToken);
                await copy.WriteAsync(entity.Id, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(entity.SpaceId, NpgsqlDbType.Uuid, cancellationToken);
                await WriteAsync(copy, entity.TypeId, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(entity.Name, NpgsqlDbType.Text, cancellationToken);
                await WriteAsync(copy, entity.Marking, NpgsqlDbType.Text, cancellationToken);
                await WriteAsync(copy, entity.ParentId, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(entity.HoldsItems, NpgsqlDbType.Boolean, cancellationToken);
                await WriteAsync(copy, entity.Quantity, NpgsqlDbType.Numeric, cancellationToken);
                await WriteAsync(copy, entity.Unit, NpgsqlDbType.Text, cancellationToken);
                await copy.WriteAsync(entity.Fields, NpgsqlDbType.Jsonb, cancellationToken);
                await copy.WriteAsync(entity.Tags.ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text, cancellationToken);
                await WriteAsync(copy, entity.Notes, NpgsqlDbType.Text, cancellationToken);
                await copy.WriteAsync(entity.Status.ToString(), NpgsqlDbType.Varchar, cancellationToken);
                await copy.WriteAsync(entity.CreatedAt.ToUniversalTime(), NpgsqlDbType.TimestampTz, cancellationToken);
                await copy.WriteAsync(entity.UpdatedAt.ToUniversalTime(), NpgsqlDbType.TimestampTz, cancellationToken);
                await WriteAsync(copy, entity.Value, NpgsqlDbType.Numeric, cancellationToken);
                await copy.WriteAsync(entity.ContentCount, NpgsqlDbType.Integer, cancellationToken);
                await copy.WriteAsync(entity.ContentValue, NpgsqlDbType.Numeric, cancellationToken);
                await copy.WriteAsync(entity.Reserved, NpgsqlDbType.Numeric, cancellationToken);
                await copy.WriteAsync(entity.IsExample, NpgsqlDbType.Boolean, cancellationToken);
                await copy.WriteAsync(entity.Version, NpgsqlDbType.Integer, cancellationToken);
            }

            await copy.CompleteAsync(cancellationToken);
        }

        await using (var copy = await connection.BeginBinaryImportAsync(
            """COPY "ItemPaths" ("Id", "Path", "Depth") FROM STDIN (FORMAT BINARY)""", cancellationToken))
        {
            foreach (var item in items)
            {
                await copy.StartRowAsync(cancellationToken);
                await copy.WriteAsync(item.Id, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(item.Path, NpgsqlDbType.LTree, cancellationToken);
                await copy.WriteAsync(item.Depth, NpgsqlDbType.Integer, cancellationToken);
            }

            await copy.CompleteAsync(cancellationToken);
        }

        await using (var copy = await connection.BeginBinaryImportAsync(
            """COPY "ItemEvents" ("Id", "SpaceId", "ItemId", "At", "ActorId", "Kind", "FromParent", "ToParent") FROM STDIN (FORMAT BINARY)""",
            cancellationToken))
        {
            foreach (var entity in events.Select(itemEvent => itemEvent.ToEntity()))
            {
                await copy.StartRowAsync(cancellationToken);
                await copy.WriteAsync(entity.Id, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(entity.SpaceId, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(entity.ItemId, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(entity.At.ToUniversalTime(), NpgsqlDbType.TimestampTz, cancellationToken);
                await WriteAsync(copy, entity.ActorId, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(entity.Kind.ToString(), NpgsqlDbType.Varchar, cancellationToken);
                await WriteAsync(copy, entity.FromParent, NpgsqlDbType.Uuid, cancellationToken);
                await WriteAsync(copy, entity.ToParent, NpgsqlDbType.Uuid, cancellationToken);
            }

            await copy.CompleteAsync(cancellationToken);
        }

        await using (var copy = await connection.BeginBinaryImportAsync(
            """COPY "ItemValues" ("SpaceId", "ItemId", "FieldId", "Num", "Date", "Text") FROM STDIN (FORMAT BINARY)""", cancellationToken))
        {
            foreach (var value in values)
            {
                await copy.StartRowAsync(cancellationToken);
                await copy.WriteAsync(value.SpaceId, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(value.ItemId, NpgsqlDbType.Uuid, cancellationToken);
                await copy.WriteAsync(value.FieldId, NpgsqlDbType.Uuid, cancellationToken);
                await WriteAsync(copy, value.Num, NpgsqlDbType.Numeric, cancellationToken);
                await WriteAsync(copy, value.Date, NpgsqlDbType.Date, cancellationToken);
                await WriteAsync(copy, value.Text, NpgsqlDbType.Text, cancellationToken);
            }

            await copy.CompleteAsync(cancellationToken);
        }
    }

    private static Task WriteAsync<T>(NpgsqlBinaryImporter copy, T? value, NpgsqlDbType type, CancellationToken cancellationToken)
        where T : struct =>
        value is T present ? copy.WriteAsync(present, type, cancellationToken) : copy.WriteNullAsync(cancellationToken);

    private static Task WriteAsync(NpgsqlBinaryImporter copy, string? value, NpgsqlDbType type, CancellationToken cancellationToken) =>
        value is null ? copy.WriteNullAsync(cancellationToken) : copy.WriteAsync(value, type, cancellationToken);
}
