using Invy.Core.Engines;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Seed;

/// <summary>
/// The seeded containers, top-down so each one's container is written before it: 40 places, then boxes in them down to
/// depth 6, so the things inside sit up to 8 levels deep. Also the benchmark's moving crate (1,000 boxes holding 99
/// things each, 100,000 in all) and the spare shelf it moves to.
/// </summary>
internal sealed class SeedTree
{
    public const string CrateName = "Moving crate";

    public const string ShelfName = "Spare shelf";

    public const int CrateBoxes = 1_000;

    public const int ThingsPerCrateBox = 99;

    private const int Places = 40;

    /// <summary>How the boxes below the places spread over depths 1 to 6, most at 3.</summary>
    private static readonly int[] DepthWeights = [6, 20, 30, 24, 13, 7];

    public SeedTree(Random random, Guid spaceId, SeedTypes types, int containers, DateTimeOffset now)
    {
        var levels = new List<List<Item>>();
        var top = new List<Item>();
        for (var i = 0; i < Places; i++)
        {
            top.Add(Container(random, spaceId, types.Place, null, $"{random.Pick(SeedWords.Places)} {i + 1}", now));
        }

        levels.Add(top);
        var boxes = Math.Max(containers - Places, DepthWeights.Length);
        var total = DepthWeights.Sum();
        foreach (var weight in DepthWeights)
        {
            var above = levels[^1];
            var level = new List<Item>();
            for (var i = 0; i < Math.Max(1, boxes * weight / total); i++)
            {
                var name = $"{random.Pick(SeedWords.Containers)} {random.Next(1, 500)}";
                level.Add(Container(random, spaceId, types.Box, above[random.Next(above.Count)], name, now));
            }

            levels.Add(level);
        }

        Crate = Container(random, spaceId, types.Box, top[0], CrateName, now);
        Shelf = Container(random, spaceId, types.Box, top[1], ShelfName, now);
        CrateBoxList = Enumerable.Range(1, CrateBoxes)
            .Select(i => Container(random, spaceId, types.Box, Crate, $"Crate box {i}", now))
            .ToList();
        Shelves = [.. levels.SelectMany(level => level)];
        All = [.. Shelves, Crate, Shelf, .. CrateBoxList];
    }

    /// <summary>Every container, each after its own container.</summary>
    public IReadOnlyList<Item> All { get; }

    /// <summary>The containers the ordinary things go into: all but the crate, its boxes and the spare shelf.</summary>
    public IReadOnlyList<Item> Shelves { get; }

    public Item Crate { get; }

    public Item Shelf { get; }

    public IReadOnlyList<Item> CrateBoxList { get; }

    private static Item Container(Random random, Guid spaceId, ItemType type, Item? parent, string name, DateTimeOffset now)
    {
        var created = now.AddDays(-random.Next(730, 1095));
        var item = new Item
        {
            Id = Guid.CreateVersion7(created),
            SpaceId = spaceId,
            TypeId = type.Id,
            Name = name,
            Marking = random.Next(4) == 0 ? $"B-{random.Next(1000, 9999)}" : null,
            HoldsItems = true,
            CreatedAt = created,
            UpdatedAt = created,
        };
        ItemEngine.Place(item, parent);
        return item;
    }
}
