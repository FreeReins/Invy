namespace Invy.Bench;

/// <summary>
/// From the command line: <c>--connection</c>, <c>--runs</c> (per search), <c>--out</c> (a Markdown file) and <c>--backup</c>
/// (back up and restore the database instead).
/// </summary>
internal sealed record BenchOptions(string Connection, int Runs, string? Out, bool Backup)
{
    public const string DefaultConnection = "Host=localhost;Port=5433;Database=invy;Username=invy;Password=invy;Command Timeout=300";

    public static BenchOptions Parse(string[] args)
    {
        var options = new BenchOptions(DefaultConnection, 40, null, false);
        for (var i = 0; i < args.Length; i++)
        {
            options = args[i] switch
            {
                "--connection" => options with { Connection = args[++i] },
                "--runs" => options with { Runs = int.Parse(args[++i]) },
                "--out" => options with { Out = args[++i] },
                "--backup" => options with { Backup = true },
                _ => throw new ArgumentException($"Unknown option {args[i]}. Use --connection, --runs, --out or --backup."),
            };
        }

        return options;
    }
}
