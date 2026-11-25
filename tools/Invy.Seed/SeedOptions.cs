namespace Invy.Seed;

/// <summary>What to seed, from the command line: <c>--items</c>, <c>--events</c>, <c>--connection</c>, <c>--seed</c>, <c>--reset</c>.</summary>
internal sealed record SeedOptions(string Connection, int Items, long Events, int Seed, bool Reset)
{
    /// <summary>The benchmark database's address: a throwaway PostgreSQL on port 5433 (see docs/reference/limits.md).</summary>
    public const string DefaultConnection = "Host=localhost;Port=5433;Database=invy;Username=invy;Password=invy;Command Timeout=0";

    public static SeedOptions Parse(string[] args)
    {
        var options = new SeedOptions(DefaultConnection, 5_000_000, 50_000_000, 7, false);
        for (var i = 0; i < args.Length; i++)
        {
            options = args[i] switch
            {
                "--connection" => options with { Connection = args[++i] },
                "--items" => options with { Items = int.Parse(args[++i].Replace("_", string.Empty)) },
                "--events" => options with { Events = long.Parse(args[++i].Replace("_", string.Empty)) },
                "--seed" => options with { Seed = int.Parse(args[++i]) },
                "--reset" => options with { Reset = true },
                _ => throw new ArgumentException($"Unknown option {args[i]}. Use --items, --events, --connection, --seed or --reset."),
            };
        }

        if (options.Events < options.Items)
        {
            throw new ArgumentException("Every item has a Created event, so --events must be at least --items.");
        }

        return options;
    }
}
