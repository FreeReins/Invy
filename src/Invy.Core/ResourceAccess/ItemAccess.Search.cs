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

        // Only a relevance order needs each match scored; the others are kept by an index and stop at the limit.
        var rank = query.Sort == SearchOrder.Relevance ? sql.Rank(query) : "0::real";

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
        // Far above the cap, the estimate is shown without counting: counting reads every matching row up to the cap.
        var estimate = await EstimateAsync(spaceId, query, cancellationToken);
        if (estimate > 4L * cap)
        {
            return new SearchCount((int)Math.Min(int.MaxValue, estimate), true);
        }

        var counting = new SearchSql(spaceId);
        var count = 0;
        await ReadAsync(
            $"SELECT count(*)::int FROM (SELECT 1 FROM \"Items\" i WHERE {counting.Where(query)} LIMIT {counting.Add(cap + 1)}) c",
            counting,
            async reader => count = await reader.ReadAsync(cancellationToken) ? reader.GetInt32(0) : 0,
            cancellationToken);
        return count <= cap
            ? new SearchCount(count, false)
            : new SearchCount((int)Math.Min(int.MaxValue, Math.Max(cap + 1, estimate)), true);
    }

    /// <summary>The database's estimate of how many items match, read from its plan without running it.</summary>
    private async Task<long> EstimateAsync(Guid spaceId, ItemQuery query, CancellationToken cancellationToken)
    {
        var estimating = new SearchSql(spaceId);
        long estimate = 0;
        await ReadAsync(
            $"EXPLAIN (FORMAT JSON) SELECT 1 FROM \"Items\" i WHERE {estimating.Where(query)}",
            estimating,
            async reader =>
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    using var plan = JsonDocument.Parse(reader.GetString(0));
                    estimate = (long)plan.RootElement[0].GetProperty("Plan").GetProperty("Plan Rows").GetDouble();
                }
            },
            cancellationToken);
        return estimate;
    }

    public async Task RecountAsync(Guid spaceId, IReadOnlyCollection<Guid> containerIds, CancellationToken cancellationToken)
    {
        if (containerIds.Count == 0)
        {
            return;
        }

        // Each container adds up its direct children (their own count and value, plus what their rollups hold), deepest
        // first, so the children it reads are already right. That reads only direct children, however many things
        // are further down. The containers named are all of the changed things' ancestors, so the rest are up to date.
        var ids = containerIds.ToArray();
        var levels = await db.ItemPaths.AsNoTracking()
            .Where(path => ids.Contains(path.Id))
            .GroupBy(path => path.Depth)
            .Select(level => new { Depth = level.Key, Ids = level.Select(path => path.Id).ToList() })
            .ToListAsync(cancellationToken);
        foreach (var level in levels.OrderByDescending(level => level.Depth))
        {
            var levelIds = level.Ids.ToArray();
            await db.Database.ExecuteSqlAsync(
                $"""
                UPDATE "Items" AS c
                SET "ContentCount" = t.n, "ContentValue" = t.v
                FROM (
                    SELECT p."Id",
                        coalesce(sum(CASE WHEN ch."Status" IN ('Disposed', 'Archived') THEN 0 ELSE 1 END + ch."ContentCount"), 0)::int AS n,
                        coalesce(sum(CASE WHEN ch."Status" IN ('Disposed', 'Archived') THEN 0 ELSE coalesce(ch."Value", 0) END + ch."ContentValue"), 0) AS v
                    FROM unnest({levelIds}) AS p("Id")
                    LEFT JOIN "Items" ch ON ch."SpaceId" = {spaceId} AND ch."ParentId" = p."Id"
                    GROUP BY p."Id") t
                WHERE c."Id" = t."Id" AND c."SpaceId" = {spaceId} AND (c."ContentCount" <> t.n OR c."ContentValue" <> t.v)
                """,
                cancellationToken);
        }
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
