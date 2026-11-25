// Seeds a throwaway database for the scale benchmark. Dev only. See docs/reference/limits.md.
//
//   dotnet run -c Release --project tools/Invy.Seed -- [--items 5000000] [--events 50000000] [--reset]
//       [--connection "Host=localhost;Port=5433;..."] [--seed 7]
using Invy.Seed;

using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancel.Cancel();
};

try
{
    await new Seeder(SeedOptions.Parse(args)).RunAsync(cancel.Token);
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
