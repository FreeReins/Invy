using System.Diagnostics;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Persistence;
using Invy.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Invy.Seed;

/// <summary>
/// Fills the database with one space, <see cref="SpaceName"/>, owned by <see cref="Username"/>: containers to depth 6,
/// things to depth 7 (8 levels), and their timelines, then rebuilds the indexes and the containers' rollups through
/// <see cref="ItemAccess.RecountAsync"/>, as the rollups queue would.
/// </summary>
internal sealed class Seeder(SeedOptions options)
{
    public const string SpaceName = "Seeded";

    public const string Username = "bench";

    public const string Password = "bench-password";

    /// <summary>The share of items that are containers.</summary>
    private const double ContainerShare = 0.05;

    /// <summary>A few things with very long timelines, for the timeline benchmark's worst case.</summary>
    private const int BusyThings = 10;

    private const int BusyEvents = 20_000;

    private readonly DateTimeOffset now = DateTimeOffset.UtcNow;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var total = Stopwatch.StartNew();
        await using (var db = Context())
        {
            Console.WriteLine("Updating the schema...");
            await DatabaseUpgrade.RunAsync(db, NullLogger.Instance, cancellationToken);
            await ClearAsync(db, cancellationToken);
        }

        var random = new Random(options.Seed);
        var (spaceId, userId, types) = await CreateSpaceAsync(cancellationToken);

        Console.WriteLine("Dropping the item tables' secondary indexes for the load...");
        var indexes = await SeedIndexes.DropAsync(options.Connection, cancellationToken);

        var step = Stopwatch.StartNew();
        var tree = new SeedTree(random, spaceId, types, (int)(options.Items * ContainerShare), now);
        var things = new SeedThings(random, spaceId, userId, types, now);
        var thingCount = options.Items - tree.All.Count;
        var crateThings = SeedTree.CrateBoxes * SeedTree.ThingsPerCrateBox;
        var busy = options.Events >= 10L * options.Items ? Math.Min(BusyThings, thingCount) : 0;
        var mean = Math.Max(0, (double)(options.Events - options.Items - ((long)busy * BusyEvents)) / options.Items);
        Console.WriteLine($"Copying {options.Items:N0} items ({tree.All.Count:N0} containers) and about {options.Events:N0} events...");
        await using (var writer = await SeedWriter.OpenAsync(options.Connection, cancellationToken))
        {
            foreach (var container in tree.All)
            {
                writer.Write(container, [], things.Timeline(container, Extra(random, mean), tree.Shelves));
            }

            // The busy things first, then the crate's, then the rest on the shelves.
            for (var i = 0; i < thingCount; i++)
            {
                var inCrate = i >= busy && i - busy < crateThings;
                var container = inCrate ? tree.CrateBoxList[(i - busy) / SeedTree.ThingsPerCrateBox] : tree.Shelves[random.Next(tree.Shelves.Count)];
                var thing = things.Thing(container);
                var values = things.Values(thing);
                var extra = i < busy ? BusyEvents : Extra(random, mean);
                writer.Write(thing, values, things.Timeline(thing, extra, tree.Shelves));
                if ((i + 1) % 500_000 == 0)
                {
                    Console.WriteLine($"  {writer.Items:N0} items, {writer.Events:N0} events ({step.Elapsed.TotalSeconds:0}s)");
                }
            }

            await writer.CompleteAsync(cancellationToken);
            Console.WriteLine($"Copied {writer.Items:N0} items, {writer.Events:N0} events and {writer.Values:N0} values in {step.Elapsed.TotalSeconds:0}s.");
        }

        step.Restart();
        Console.WriteLine($"Building {indexes.Count} indexes...");
        await SeedIndexes.CreateAsync(options.Connection, indexes, cancellationToken);
        await ExecuteAsync("""VACUUM (ANALYZE) "Items", "ItemPaths", "ItemEvents", "ItemValues" """, cancellationToken);
        Console.WriteLine($"Built the indexes and analyzed in {step.Elapsed.TotalSeconds:0}s.");

        step.Restart();
        await RecountAsync(spaceId, tree.All.Select(container => container.Id).ToList(), cancellationToken);
        await ExecuteAsync("""VACUUM (ANALYZE) "Items", "ItemPaths" """, cancellationToken);
        Console.WriteLine($"Counted the rollups of {tree.All.Count:N0} containers in {step.Elapsed.TotalSeconds:0}s.");
        Console.WriteLine($"Done in {total.Elapsed.TotalMinutes:0.0} minutes. Sign in as {Username} / {Password} to look around.");
    }

    /// <summary>An exponential spread around <paramref name="mean"/>: most things have a few events, some have many.</summary>
    private static int Extra(Random random, double mean) =>
        (int)Math.Min(2_000, Math.Round(-Math.Log(1 - random.NextDouble()) * mean));

    private InvyDbContext Context() =>
        new(new DbContextOptionsBuilder<InvyDbContext>().UseNpgsql(options.Connection).Options);

    private async Task ClearAsync(InvyDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(cancellationToken) && !await db.Spaces.AnyAsync(cancellationToken))
        {
            return;
        }

        if (!options.Reset)
        {
            throw new InvalidOperationException(
                "This database already has users or spaces. Seed an empty database, or add --reset to delete everything in this one first.");
        }

        Console.WriteLine("Deleting everything already in the database (--reset)...");
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE "Users", "Spaces" CASCADE""", cancellationToken);
    }

    private async Task<(Guid SpaceId, Guid UserId, SeedTypes Types)> CreateSpaceAsync(CancellationToken cancellationToken)
    {
        await using var db = Context();
        var user = new UserAccount
        {
            Username = Username,
            DisplayName = "Benchmark",
            PasswordHash = new IdentityPasswordHasher().Hash(Password),
            IsAdmin = true,
            CreatedAt = now,
        };
        var space = new Space
        {
            Name = SpaceName,
            Preset = "business",
            CreatedAt = now,
            Features = [.. SpaceFeatures.All.Select(feature => feature.Key)],
            Members = [new SpaceMember { UserId = user.Id, Role = SpaceRole.Owner, GettingStartedDismissed = true }],
        };
        await new AccountAccess(db).SaveUserAsync(user, cancellationToken);
        await new SpaceAccess(db).SaveAsync(space, cancellationToken);
        var types = new SeedTypes(space.Id);
        var typeAccess = new TypeAccess(db);
        foreach (var type in types.All)
        {
            await typeAccess.SaveAsync(type, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return (space.Id, user.Id, types);
    }

    /// <summary>The rollups of every container, as the rollups queue counts them: deepest first, from each one's children.</summary>
    private async Task RecountAsync(Guid spaceId, List<Guid> containers, CancellationToken cancellationToken)
    {
        await using var db = Context();
        await new ItemAccess(db).RecountAsync(spaceId, containers, cancellationToken);
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(options.Connection);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
