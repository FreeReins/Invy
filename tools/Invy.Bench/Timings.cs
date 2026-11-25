namespace Invy.Bench;

/// <summary>How long each run of one benchmark took, in milliseconds, and its percentiles (nearest rank).</summary>
internal sealed class Timings(string name)
{
    private readonly List<double> runs = [];

    public string Name { get; } = name;

    /// <summary>For a search: how many things it matched (estimated above 10,000), from its first page.</summary>
    public int? Matches { get; set; }

    /// <summary>For a search with words: how many things its words alone match. That, not its result, sets its cost.</summary>
    public int? WordMatches { get; set; }

    public int Count => runs.Count;

    public double P50 => Percentile(50);

    public double P95 => Percentile(95);

    public double Max => runs.Count == 0 ? 0 : runs.Max();

    public void Add(TimeSpan elapsed) => runs.Add(elapsed.TotalMilliseconds);

    public void AddRange(Timings other) => runs.AddRange(other.runs);

    public double Percentile(int percent)
    {
        if (runs.Count == 0)
        {
            return 0;
        }

        var sorted = runs.Order().ToList();
        var rank = (int)Math.Ceiling(percent / 100.0 * sorted.Count);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
    }
}
