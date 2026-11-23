using System.Data.Common;
using System.Text.Json;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Invy.Core.ResourceAccess;

/// <content>Search (SQL from the query tree in <see cref="SearchSql"/>) and the containers' rollups.</content>
internal sealed partial class ItemAccess
{
    public async Task<List<SearchHit>> SearchAsync(Guid spaceId, ItemQuery query, SearchHit? after, int limit, CancellationToken cancellationToken)
    {
        var sql = new SearchSql(spaceId);
        var where = sql.Where(query);
        var rank = sql.Rank(query);

        // Keyset paging: each order has a key that ends in the id, and the next page starts after the last row's key.
        var (order, next) = query.Sort switch
        {
            SearchOrder.Relevance => ("s.\"Rank\" DESC, s.\"Id\"", after is null ? null
                : $"(s.\"Rank\" < {sql.Add(after.Rank)}::real OR (s.\"Rank\" = {sql.Add(after.Rank)}::real AND s.\"Id\" > {sql.Add(after.Id)}))"),
            SearchOrder.Recent => ("s.\"UpdatedAt\" DESC, s.\"Id\" DESC", after is null ? null
                : $"(s.\"UpdatedAt\", s.\"Id\") < ({sql.Add(after.UpdatedAt.ToUniversalTime())}, {sql.Add(after.Id)})"),
            _ => ("s.\"Name\", s.\"Id\"", after is null ? null : $"(s.\"Name\", s.\"Id\") > ({sql.Add(after.Name)}, {sql.Add(after.Id)})"),
        };

        var text = $"""
            SELECT s."Id", s."Rank", s."Name", s."UpdatedAt"
            FROM (SELECT i."Id", i."Name", i."UpdatedAt", {rank} AS "Rank" FROM "Items" i WHERE {where}) s
            {(next is null ? string.Empty : $"WHERE {next}")}
            ORDER BY {order}
            LIMIT {sql.Add(limit)}
            """;

        var hits = new List<SearchHit>();
        await ReadAsync(text, sql, async reader =>
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                hits.Add(new SearchHit(reader.GetGuid(0), reader.GetFloat(1), reader.GetString(2), reader.GetFieldValue<DateTimeOffset>(3)));
            }
        }, cancellationToken);
        return hits;
    }

    public async Task<SearchCount> CountAsync(Guid spaceId, ItemQuery query, int cap, CancellationToken cancellationToken)
    {
        var counting = new SearchSql(spaceId);
        var count = 0;
        await ReadAsync(
            $"SELECT count(*)::int FROM (SELECT 1 FROM \"Items\" i WHERE {counting.Where(query)} LIMIT {counting.Add(cap + 1)}) c",
            counting,
            async reader => count = await reader.ReadAsync(cancellationToken) ? reader.GetInt32(0) : 0,
            cancellationToken);
        if (count <= cap)
        {
            return new SearchCount(count, false);
        }

        // Above the cap, counting every row costs too much: the planner's estimate is shown instead.
        var estimating = new SearchSql(spaceId);
        var estimate = count;
        await ReadAsync(
            $"EXPLAIN (FORMAT JSON) SELECT 1 FROM \"Items\" i WHERE {estimating.Where(query)}",
            estimating,
            async reader =>
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    using var plan = JsonDocument.Parse(reader.GetString(0));
                    estimate = (int)Math.Min(int.MaxValue, Math.Max(cap + 1, plan.RootElement[0].GetProperty("Plan").GetProperty("Plan Rows").GetDouble()));
                }
            },
            cancellationToken);
        return new SearchCount(estimate, true);
    }

    public Task RecountAsync(Guid spaceId, IReadOnlyCollection<Guid> containerIds, CancellationToken cancellationToken)
    {
        var ids = containerIds.ToArray();
        return db.Database.ExecuteSqlAsync(
            $"""
            UPDATE "Items" AS c
            SET "ContentCount" = t.n, "ContentValue" = t.v
            FROM (
                SELECT c2."Id", count(d."Id")::int AS n, coalesce(sum(d."Value"), 0) AS v
                FROM "Items" c2
                LEFT JOIN "Items" d ON d."SpaceId" = c2."SpaceId" AND d."Path" <@ c2."Path" AND d."Id" <> c2."Id"
                    AND d."Status" NOT IN ('Disposed', 'Archived')
                WHERE c2."SpaceId" = {spaceId} AND c2."Id" = ANY({ids})
                GROUP BY c2."Id") t
            WHERE c."Id" = t."Id" AND (c."ContentCount" <> t.n OR c."ContentValue" <> t.v)
            """,
            cancellationToken);
    }

    /// <summary>Runs a query built by <see cref="SearchSql"/> on the context's connection and hands its reader to <paramref name="read"/>.</summary>
    private async Task ReadAsync(string text, SearchSql sql, Func<DbDataReader, Task> read, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var opened = connection.State != System.Data.ConnectionState.Open;
        if (opened)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = text;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            command.Parameters.AddRange(sql.Parameters.ToArray());
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await read(reader);
        }
        finally
        {
            if (opened)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
