using Npgsql;

namespace Invy.Seed;

/// <summary>
/// Drops the item tables' secondary indexes before the COPY and builds them again after it, which is many times
/// faster than updating them row by row. Primary keys stay. The definitions come from the database, so they match
/// the migrations whatever those add.
/// </summary>
internal static class SeedIndexes
{
    private const string Find = """
        SELECT i.indexrelid::regclass::text, pg_get_indexdef(i.indexrelid)
        FROM pg_index i
        JOIN pg_class t ON t.oid = i.indrelid
        WHERE t.relname IN ('Items', 'ItemPaths', 'ItemEvents', 'ItemValues') AND t.relnamespace = 'public'::regnamespace
            AND NOT i.indisprimary AND NOT EXISTS (SELECT 1 FROM pg_constraint c WHERE c.conindid = i.indexrelid)
        ORDER BY 1
        """;

    public static async Task<List<string>> DropAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var found = new List<(string Name, string Definition)>();
        await using (var command = new NpgsqlCommand(Find, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                found.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        foreach (var (name, _) in found)
        {
            await using var drop = new NpgsqlCommand($"DROP INDEX {name}", connection);
            await drop.ExecuteNonQueryAsync(cancellationToken);
        }

        return found.Select(index => index.Definition).ToList();
    }

    /// <summary>Builds the indexes two at a time, each with 1 GB to sort in.</summary>
    public static async Task CreateAsync(string connectionString, IReadOnlyList<string> definitions, CancellationToken cancellationToken)
    {
        var queue = new Queue<string>(definitions.OrderBy(definition => definition.Contains("\"ItemEvents\"") ? 0 : 1));
        var workers = Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using (var settings = new NpgsqlCommand("SET maintenance_work_mem = '1GB'", connection))
            {
                await settings.ExecuteNonQueryAsync(cancellationToken);
            }

            while (true)
            {
                string definition;
                lock (queue)
                {
                    if (!queue.TryDequeue(out definition!))
                    {
                        return;
                    }
                }

                var started = DateTime.UtcNow;
                await using var create = new NpgsqlCommand(definition, connection);
                await create.ExecuteNonQueryAsync(cancellationToken);
                Console.WriteLine($"  {definition[..Math.Min(definition.Length, 110)]} ({(DateTime.UtcNow - started).TotalSeconds:0}s)");
            }
        });
        await Task.WhenAll(workers);
    }
}
