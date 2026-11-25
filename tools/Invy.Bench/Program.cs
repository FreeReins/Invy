// The scale benchmark, against a database seeded by tools/Invy.Seed. Dev only. See docs/reference/limits.md.
//
//   dotnet run -c Release --project tools/Invy.Bench -- [--runs 40] [--out results.md] [--connection "..."]
//   dotnet run -c Release --project tools/Invy.Bench -- --backup [--out backup.md]
//
// Exits with 2 when the results miss the design's targets.
using Invy.Bench;

using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancel.Cancel();
};

try
{
    var options = BenchOptions.Parse(args);
    using var benchmark = new Benchmark(options);
    if (options.Backup)
    {
        var backup = await benchmark.RunBackupAsync(cancel.Token);
        Console.WriteLine();
        Console.WriteLine(backup);
        if (options.Out is { } file)
        {
            await File.WriteAllTextAsync(file, backup);
        }

        return 0;
    }

    var report = await benchmark.RunAsync(cancel.Token);
    var markdown = report.ToMarkdown();
    Console.WriteLine();
    Console.WriteLine(markdown);
    if (options.Out is { } path)
    {
        await File.WriteAllTextAsync(path, markdown);
    }

    return report.MeetsTargets ? 0 : 2;
}
catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
