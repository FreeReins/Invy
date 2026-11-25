using System.Globalization;
using System.Text;

namespace Invy.Bench;

/// <summary>The benchmark's results, written as Markdown tables for docs/reference/limits.md.</summary>
internal sealed class BenchReport(BenchSamples samples, string server)
{
    public List<Timings> Searches { get; } = [];

    public List<Timings> Timelines { get; } = [];

    public Timings CrateMoves { get; } = new("Move a container holding 100,000 things");

    public Timings CrateRollups { get; } = new("Rollups catching up after it");

    public Timings ThingMoves { get; } = new("Move one thing");

    public Timings ThingRollups { get; } = new("Rollups catching up after it");

    public Dictionary<string, double> EventsPerMove { get; } = [];

    public int ShelfCountAfter { get; set; }

    public int CrateCount { get; set; }

    /// <summary>True when search and timelines meet the design's targets at p95.</summary>
    public bool MeetsTargets => NarrowSearches.P95 < Benchmark.SearchTarget && Timelines.All(timings => timings.P95 < Benchmark.TimelineTarget);

    private Timings NarrowSearches => Combined(
        $"Searches without words, or whose words match up to {Benchmark.NarrowSearch:N0}", timings => (timings.WordMatches ?? 0) <= Benchmark.NarrowSearch);

    private Timings BroadSearches => Combined(
        $"Searches whose words match more than {Benchmark.NarrowSearch:N0}", timings => (timings.WordMatches ?? 0) > Benchmark.NarrowSearch);

    /// <summary>The runs of several searches together; <see cref="Timings.Matches"/> stays empty.</summary>
    private Timings Combined(string name, Func<Timings, bool> which)
    {
        var all = new Timings(name);
        Searches.Where(which).ToList().ForEach(all.AddRange);
        return all;
    }

    public string ToMarkdown()
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"Measured {DateTimeOffset.UtcNow:yyyy-MM-dd} on {Machine()}.");
        text.AppendLine(CultureInfo.InvariantCulture, $"Server: {server}.");
        text.AppendLine(CultureInfo.InvariantCulture, $"Data: {samples.Items:N0} items and {samples.Events:N0} events in one space.");
        text.AppendLine();
        text.AppendLine("| Search | Matches | Its words match | Runs | p50 ms | p95 ms | max ms |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var timings in Searches.Append(NarrowSearches).Append(BroadSearches))
        {
            Row(text, timings, search: true);
        }

        text.AppendLine();
        text.AppendLine("| Timeline | Runs | p50 ms | p95 ms | max ms |");
        text.AppendLine("|---|---:|---:|---:|---:|");
        Timelines.ForEach(timings => Row(text, timings));
        text.AppendLine();
        text.AppendLine("| Move | Runs | p50 ms | p95 ms | max ms |");
        text.AppendLine("|---|---:|---:|---:|---:|");
        foreach (var timings in new[] { CrateMoves, CrateRollups, ThingMoves, ThingRollups })
        {
            Row(text, timings);
        }

        text.AppendLine();
        text.AppendLine(CultureInfo.InvariantCulture,
            $"Events written per move: {EventsPerMove.GetValueOrDefault("crate"):0.#} for the crate, {EventsPerMove.GetValueOrDefault("thing"):0.#} for one thing. "
            + $"The crate holds {CrateCount:N0}; the spare shelf held {ShelfCountAfter:N0} after the last move back.");
        text.AppendLine();
        text.AppendLine(MeetsTargets
            ? $"Meets the targets: p95 under {Benchmark.SearchTarget} ms for searches whose words match up to {Benchmark.NarrowSearch:N0}, timelines p95 under {Benchmark.TimelineTarget} ms."
            : $"Misses the targets: p95 must be under {Benchmark.SearchTarget} ms for searches whose words match up to {Benchmark.NarrowSearch:N0}, and timelines under {Benchmark.TimelineTarget} ms.");
        return text.ToString();
    }

    /// <summary>One row; in the search table, with how many things the search matched (blank for the totals).</summary>
    private static void Row(StringBuilder text, Timings timings, bool search = false) =>
        text.AppendLine(CultureInfo.InvariantCulture,
            $"| {timings.Name} |{(search ? $" {timings.Matches:N0} | {timings.WordMatches:N0} |" : string.Empty)} {timings.Count} | {timings.P50:0.0} | {timings.P95:0.0} | {timings.Max:0.0} |");

    private static string Machine()
    {
        var cpu = File.Exists("/proc/cpuinfo")
            ? File.ReadLines("/proc/cpuinfo").FirstOrDefault(line => line.StartsWith("model name", StringComparison.Ordinal))?.Split(':', 2)[1].Trim()
            : null;
        var memory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
        return $"{cpu ?? "an unknown CPU"}, {Environment.ProcessorCount} logical cores, {memory:0} GB memory (the benchmark's machine)";
    }
}
