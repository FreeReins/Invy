using System.Diagnostics;
using System.Text.RegularExpressions;
using Invy.Core;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Invy.Bench;

/// <summary>
/// The scale benchmark (docs/developers/design.md, Scale targets). Every call goes through the real manager, in its
/// own scope as an HTTP request would, so the timings include validation, parsing, SQL and mapping.
/// </summary>
internal sealed class Benchmark(BenchOptions options) : IDisposable
{
    /// <summary>The design's targets, in milliseconds.</summary>
    public const double SearchTarget = 200;

    public const double TimelineTarget = 100;

    /// <summary>The search target covers searches whose words match up to this many things; broader words read every match.</summary>
    public const int NarrowSearch = 20_000;

    private readonly ServiceProvider services = Services(options.Connection);

    /// <summary>Backs up and restores the database instead (<c>--backup</c>).</summary>
    public Task<string> RunBackupAsync(CancellationToken cancellationToken) =>
        new BenchBackup(services, options.Connection).RunAsync(cancellationToken);

    public async Task<BenchReport> RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Reading samples from the seeded database...");
        var samples = await BenchSamples.LoadAsync(options.Connection, cancellationToken);
        var report = new BenchReport(samples, await ServerAsync(cancellationToken));

        Console.WriteLine($"Searching ({options.Runs} runs of each)...");
        foreach (var (name, text, sort) in Searches(samples))
        {
            var timings = new Timings(name);
            for (var run = -1; run < options.Runs; run++)
            {
                SearchResponse? found = null;
                var elapsed = await TimeAsync(async manager =>
                    found = await manager.SearchAsync(samples.UserId, samples.SpaceId, text(run), sort, null, cancellationToken));
                timings.Matches = Math.Max(timings.Matches ?? 0, found?.Total ?? 0);
                if (run >= 0)
                {
                    timings.Add(elapsed);
                }
            }

            if (Words(text(0)) is { Length: > 0 } words)
            {
                await using var scope = services.CreateAsyncScope();
                var alone = await scope.ServiceProvider.GetRequiredService<IItemManager>()
                    .SearchAsync(samples.UserId, samples.SpaceId, words, null, null, cancellationToken);
                timings.WordMatches = alone.Total;
            }

            report.Searches.Add(timings);
        }

        report.Searches.Add(await SecondPagesAsync(samples, cancellationToken));

        Console.WriteLine("Reading timelines...");
        report.Timelines.Add(await HistoriesAsync("A thing's timeline", samples, samples.Things, cancellationToken));
        report.Timelines.Add(await HistoriesAsync("A container's timeline", samples, samples.Containers, cancellationToken));
        report.Timelines.Add(await HistoriesAsync("A thing with 20,000 events", samples, samples.Busy, cancellationToken));
        var activity = new Timings("Space activity, first page");
        for (var run = 0; run < options.Runs; run++)
        {
            activity.Add(await TimeAsync(manager => manager.ListActivityAsync(samples.UserId, samples.SpaceId, null, cancellationToken)));
        }

        report.Timelines.Add(activity);

        Console.WriteLine("Moving the crate of 100,000 things, and one thing...");
        await MoveAsync(report, samples, cancellationToken);
        return report;
    }

    public void Dispose() => services.Dispose();

    private static ServiceProvider Services(string connection)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Invy"] = connection,
                ["Data:Root"] = Path.Combine(AppContext.BaseDirectory, "bench-data"),
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInvyCore(configuration);
        services.AddSingleton<BenchOutbox>();
        services.AddScoped<IMessageAccess, BenchMessages>();
        return services.BuildServiceProvider();
    }

    /// <summary>The plain words of a search, without its terms (type:, in:, price>50).</summary>
    private static string Words(string text) =>
        string.Join(' ', Regex.Replace(text, "\\S+:\"[^\"]*\"", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.Contains(':') && !part.Contains('>') && !part.Contains('<')));

    /// <summary>Realistic searches; a few rotate their words between runs so each run isn't the same query.</summary>
    private static IEnumerable<(string Name, Func<int, string> Text, string? Sort)> Searches(BenchSamples samples)
    {
        string[] words = ["drill", "hammer", "socket set", "multimeter", "bolt", "hinge", "ladder", "capacitor"];
        string[] pairs = ["cordless drill", "brass hinge", "red hammer", "spare battery", "steel bracket"];
        string[] typos = ["drll", "hamer", "screwdrivr", "multimter", "capacitr"];
        Func<int, string> place = run => samples.Places[Math.Abs(run) % samples.Places.Count];
        Func<int, string> rare = run => samples.Markings[Math.Abs(run) % samples.Markings.Count];
        return
        [
            ("A rare word (a marking)", rare, null),
            ("A rare word and type:part", run => $"{rare(run)} type:part", null),
            ("A rare word in:\"<place>\"", run => $"{rare(run)} in:\"{place(run)}\"", null),
            ("One word", run => words[Math.Abs(run) % words.Length], null),
            ("Two words", run => pairs[Math.Abs(run) % pairs.Length], null),
            ("A typo (trigram)", run => typos[Math.Abs(run) % typos.Length], null),
            ("One word, newest first", run => words[Math.Abs(run) % words.Length], "recent"),
            ("type:tool", _ => "type:tool", null),
            ("brand:makita", _ => "brand:makita", null),
            ("price>100", _ => "price>100", null),
            ("type:tool brand:dewalt price>50", _ => "type:tool brand:dewalt price>50", null),
            ("bolt qty<5 size>10", _ => "bolt qty<5 size>10", null),
            ("in:\"<place>\"", run => $"in:\"{place(run)}\"", null),
            ("in:\"<place>\" hammer", run => $"in:\"{place(run)}\" hammer", null),
            ("seen>1y", _ => "seen>1y", null),
            ("added<30d", _ => "added<30d", null),
            ("is:lent", _ => "is:lent", null),
            ("tag:garden drill", _ => "tag:garden drill", null),
            ("Everything, by name", _ => string.Empty, null),
        ];
    }

    private async Task<Timings> SecondPagesAsync(BenchSamples samples, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var first = await scope.ServiceProvider.GetRequiredService<IItemManager>()
            .SearchAsync(samples.UserId, samples.SpaceId, "type:tool", null, null, cancellationToken);
        var timings = new Timings("type:tool, second page") { Matches = first.Total };
        for (var run = 0; run < options.Runs; run++)
        {
            timings.Add(await TimeAsync(manager => manager.SearchAsync(samples.UserId, samples.SpaceId, "type:tool", null, first.Next, cancellationToken)));
        }

        return timings;
    }

    private async Task<Timings> HistoriesAsync(string name, BenchSamples samples, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var timings = new Timings(name);
        foreach (var id in ids)
        {
            timings.Add(await TimeAsync(manager => manager.GetHistoryAsync(samples.UserId, samples.SpaceId, id, cancellationToken)));
        }

        return timings;
    }

    /// <summary>
    /// Moves the crate to the spare shelf and back three times, then one thing the same way, timing each move, how many
    /// events it wrote, and the rollups queue catching up on what it published.
    /// </summary>
    private async Task MoveAsync(BenchReport report, BenchSamples samples, CancellationToken cancellationToken)
    {
        var outbox = services.GetRequiredService<BenchOutbox>();
        outbox.Take<ItemsChanged>();
        var thing = samples.Things[0];
        var thingHome = await ScalarAsync<Guid>("""SELECT "ParentId" FROM "Items" WHERE "Id" = @id""", thing, cancellationToken);
        foreach (var (id, home, moves, rollups) in new[]
        {
            (samples.Crate, samples.CrateHome, report.CrateMoves, report.CrateRollups),
            (thing, thingHome, report.ThingMoves, report.ThingRollups),
        })
        {
            var eventsBefore = await ScalarAsync<long>("""SELECT count(*) FROM "ItemEvents" WHERE "SpaceId" = @id""", samples.SpaceId, cancellationToken);
            for (var run = 0; run < 6; run++)
            {
                var to = run % 2 == 0 ? samples.Shelf : home;
                moves.Add(await TimeAsync(manager => manager.MoveItemAsync(samples.UserId, samples.SpaceId, id, new MoveItemRequest(to), cancellationToken)));
                var caughtUp = Stopwatch.StartNew();
                foreach (var message in outbox.Take<ItemsChanged>())
                {
                    await using var scope = services.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IItemManager>().UpdateRollupsAsync(message, cancellationToken);
                }

                rollups.Add(caughtUp.Elapsed);
            }

            var eventsAfter = await ScalarAsync<long>("""SELECT count(*) FROM "ItemEvents" WHERE "SpaceId" = @id""", samples.SpaceId, cancellationToken);
            report.EventsPerMove[id == samples.Crate ? "crate" : "thing"] = (eventsAfter - eventsBefore) / 6.0;
        }

        report.ShelfCountAfter = await ScalarAsync<int>("""SELECT "ContentCount" FROM "Items" WHERE "Id" = @id""", samples.Shelf, cancellationToken);
        report.CrateCount = await ScalarAsync<int>("""SELECT "ContentCount" FROM "Items" WHERE "Id" = @id""", samples.Crate, cancellationToken);
    }

    private async Task<TimeSpan> TimeAsync<T>(Func<IItemManager, Task<T>> call)
    {
        await using var scope = services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IItemManager>();
        var watch = Stopwatch.StartNew();
        await call(manager);
        return watch.Elapsed;
    }

    private async Task<T> ScalarAsync<T>(string sql, Guid id, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(options.Connection);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task<string> ServerAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(options.Connection);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT split_part(version(), ' on ', 1) || ', shared_buffers ' || current_setting('shared_buffers')
                || ', work_mem ' || current_setting('work_mem') || ', jit ' || current_setting('jit')
                || ', database ' || pg_size_pretty(pg_database_size(current_database()))
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
