using Npgsql;

namespace Invy.Bench;

/// <summary>What the benchmark works on, read once from the seeded database (tools/Invy.Seed).</summary>
internal sealed record BenchSamples(
    Guid UserId,
    Guid SpaceId,
    Guid Crate,
    Guid CrateHome,
    Guid Shelf,
    IReadOnlyList<string> Places,
    IReadOnlyList<string> Markings,
    IReadOnlyList<Guid> Things,
    IReadOnlyList<Guid> Containers,
    IReadOnlyList<Guid> Busy,
    long Items,
    long Events)
{
    public static async Task<BenchSamples> LoadAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var user = await ScalarAsync<Guid>(connection, """SELECT "Id" FROM "Users" WHERE "Username" = 'bench'""", cancellationToken);
        var space = await ScalarAsync<Guid>(connection, """SELECT "Id" FROM "Spaces" WHERE "Name" = 'Seeded'""", cancellationToken);
        var crate = await PairsAsync(connection, """SELECT "Id", "ParentId" FROM "Items" WHERE "Name" = 'Moving crate'""", cancellationToken);
        var shelf = await ScalarAsync<Guid>(connection, """SELECT "Id" FROM "Items" WHERE "Name" = 'Spare shelf'""", cancellationToken);
        var places = await ListAsync<string>(connection, """SELECT "Name" FROM "Items" WHERE "ParentId" IS NULL AND "HoldsItems" ORDER BY "Name" LIMIT 8""", cancellationToken);
        // Things' markings ("K512") are shared by about twenty things each: rare words, as most real words are.
        var markings = await ListAsync<string>(
            connection, """SELECT "Marking" FROM "Items" TABLESAMPLE SYSTEM (0.2) REPEATABLE (7) WHERE "Marking" IS NOT NULL AND NOT "HoldsItems" LIMIT 8""", cancellationToken);
        var things = await ListAsync<Guid>(
            connection, """SELECT "Id" FROM "Items" TABLESAMPLE SYSTEM (0.2) REPEATABLE (7) WHERE NOT "HoldsItems" LIMIT 200""", cancellationToken);
        var containers = await ListAsync<Guid>(
            connection, """SELECT "Id" FROM "Items" TABLESAMPLE SYSTEM (2) REPEATABLE (7) WHERE "HoldsItems" LIMIT 50""", cancellationToken);

        // The seed gives ten things 20,000 events each: the worst case for one item's timeline.
        var busy = await ListAsync<Guid>(
            connection, """SELECT "ItemId" FROM "ItemEvents" GROUP BY "ItemId" HAVING count(*) > 10000 LIMIT 10""", cancellationToken);
        var items = await ScalarAsync<long>(connection, """SELECT count(*) FROM "Items" """, cancellationToken);
        var events = await ScalarAsync<long>(connection, """SELECT count(*) FROM "ItemEvents" """, cancellationToken);
        if (crate.Count == 0 || things.Count == 0)
        {
            throw new InvalidOperationException("This database hasn't been seeded. Run tools/Invy.Seed against it first.");
        }

        return new BenchSamples(user, space, crate[0].Id, crate[0].ParentId, shelf, places, markings, things, containers, busy, items, events);
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("This database hasn't been seeded. Run tools/Invy.Seed against it first."));
    }

    private static async Task<List<T>> ListAsync<T>(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(reader.GetFieldValue<T>(0));
        }

        return rows;
    }

    private static async Task<List<(Guid Id, Guid ParentId)>> PairsAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(Guid, Guid)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetGuid(0), reader.GetGuid(1)));
        }

        return rows;
    }
}
