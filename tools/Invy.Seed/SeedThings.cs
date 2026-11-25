using System.Text.Json;
using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Seed;

/// <summary>Makes the seeded things (tools, parts and books, with field values) and each one's timeline.</summary>
internal sealed class SeedThings(Random random, Guid spaceId, Guid userId, SeedTypes types, DateTimeOffset now)
{
    private static readonly TimeSpan History = TimeSpan.FromDays(3 * 365);

    private readonly ItemEngine engine = new();

    public Item Thing(Item container)
    {
        var created = now - (History * random.NextDouble());
        var item = new Item { Id = Guid.CreateVersion7(created), SpaceId = spaceId, CreatedAt = created, UpdatedAt = created };
        var roll = random.Next(100);
        if (roll < 45)
        {
            item.TypeId = types.Tool.Id;
            item.Name = $"{Capital(random.Pick(SeedWords.Adjectives))} {random.Pick(SeedWords.Tools)}";
            item.Fields[SeedTypes.FieldId(types.Tool, "brand")] = Json(random.Pick(SeedWords.Brands));
            item.Fields[SeedTypes.FieldId(types.Tool, "price")] = Json(Math.Round((decimal)(random.NextDouble() * 400) + 5, 2));
            item.Fields[SeedTypes.FieldId(types.Tool, "bought")] = Json(DateOnly.FromDateTime(created.UtcDateTime).ToString("yyyy-MM-dd"));
        }
        else if (roll < 80)
        {
            var size = random.Next(2, 40);
            item.TypeId = types.Part.Id;
            item.Name = $"M{size} {random.Pick(SeedWords.Parts)}";
            item.Quantity = random.Next(1, 500);
            item.Unit = "pcs";
            item.Fields[SeedTypes.FieldId(types.Part, "size")] = Json(size);
            item.Fields[SeedTypes.FieldId(types.Part, "price")] = Json(Math.Round((decimal)random.NextDouble() * 2, 2));
        }
        else
        {
            item.TypeId = types.Book.Id;
            item.Name = $"The {random.Pick(SeedWords.BookWords)} of {random.Pick(SeedWords.BookWords)}";
            item.Fields[SeedTypes.FieldId(types.Book, "author")] = Json(random.Pick(SeedWords.Authors));
            item.Fields[SeedTypes.FieldId(types.Book, "year")] = Json(random.Next(1850, 2026));
            item.Fields[SeedTypes.FieldId(types.Book, "price")] = Json(Math.Round((decimal)(random.NextDouble() * 30) + 1, 2));
        }

        if (random.Next(10) == 0)
        {
            item.Marking = $"{(char)('A' + random.Next(26))}{random.Next(100, 999)}";
        }

        if (random.Next(7) == 0)
        {
            item.Notes = random.Pick(SeedWords.Notes);
        }

        if (random.Next(4) == 0)
        {
            item.Tags.Add(random.Pick(SeedWords.Tags));
        }

        ItemEngine.Place(item, container);
        return item;
    }

    /// <summary>The item's sortable values and value, as ItemManager keeps them.</summary>
    public IReadOnlyList<ItemValue> Values(Item item)
    {
        var type = types.All.First(type => type.Id == item.TypeId);
        item.Value = engine.Value(item, type);
        return engine.SortableValues(item, type);
    }

    /// <summary>
    /// A Created event and <paramref name="extra"/> more, oldest first, ending where the item is now: moves through
    /// other containers, sightings, changes, photos and loans. Sets when it was last seen and whether it is lent out.
    /// </summary>
    public List<ItemEvent> Timeline(Item item, int extra, IReadOnlyList<Item> containers)
    {
        var times = new DateTimeOffset[extra];
        var span = (now - item.CreatedAt).Ticks;
        for (var i = 0; i < extra; i++)
        {
            times[i] = item.CreatedAt.AddTicks((long)(span * random.NextDouble()));
        }

        Array.Sort(times);
        var moves = 0;
        var kinds = new ItemEventKind[extra];
        for (var i = 0; i < extra; i++)
        {
            var roll = random.Next(100);
            kinds[i] = roll switch
            {
                < 15 => ItemEventKind.Moved,
                < 60 => ItemEventKind.Seen,
                < 85 => item.Quantity is null ? ItemEventKind.Changed : ItemEventKind.QuantityChanged,
                < 97 => ItemEventKind.PhotoAdded,
                _ => ItemEventKind.Lent,
            };
            moves += kinds[i] == ItemEventKind.Moved ? 1 : 0;
        }

        var here = moves == 0 ? item.ParentId : containers[random.Next(containers.Count)].Id;
        var events = new List<ItemEvent>(extra + 1) { Event(item, ItemEventKind.Created, item.CreatedAt, to: here) };
        var lent = false;
        for (var i = 0; i < extra; i++)
        {
            var at = times[i];
            switch (kinds[i])
            {
                case ItemEventKind.Moved:
                    moves--;
                    var to = moves == 0 ? item.ParentId : containers[random.Next(containers.Count)].Id;
                    events.Add(Event(item, ItemEventKind.Moved, at, here, to));
                    here = to;
                    break;
                case ItemEventKind.Seen:
                    events.Add(Event(item, ItemEventKind.Seen, at));
                    item.LastSeenAt = at;
                    break;
                case ItemEventKind.QuantityChanged:
                    var delta = (decimal)random.Next(-5, 10);
                    var change = Event(item, ItemEventKind.QuantityChanged, at);
                    change.QtyDelta = delta == 0 ? 1 : delta;
                    events.Add(change);
                    break;
                case ItemEventKind.Changed:
                    var changed = Event(item, ItemEventKind.Changed, at);
                    changed.Changes = [new ItemChange("Notes", null, random.Pick(SeedWords.Notes))];
                    events.Add(changed);
                    break;
                case ItemEventKind.Lent:
                    events.Add(Event(item, lent ? ItemEventKind.Returned : ItemEventKind.Lent, at));
                    lent = !lent;
                    item.LentAt = lent ? at : null;
                    break;
                default:
                    events.Add(Event(item, kinds[i], at));
                    break;
            }
        }

        // Most loans come back: about one thing in thirty is out on loan now.
        if (lent && random.Next(4) != 0)
        {
            var back = item.LentAt!.Value.AddTicks((long)((now - item.LentAt.Value).Ticks * random.NextDouble()));
            events.Add(Event(item, ItemEventKind.Returned, back));
            item.LentAt = null;
            lent = false;
        }

        if (lent)
        {
            item.Status = ItemStatus.Lent;
            item.LentTo = random.Pick(SeedWords.Authors);
        }

        item.UpdatedAt = events[^1].At;
        return events;
    }

    private static string Capital(string word) => char.ToUpperInvariant(word[0]) + word[1..];

    private static JsonElement Json<T>(T value) => JsonSerializer.SerializeToElement(value);

    private ItemEvent Event(Item item, ItemEventKind kind, DateTimeOffset at, Guid? from = null, Guid? to = null)
    {
        var made = ItemEngine.Event(item, userId, kind, at, from, to);
        made.Id = Guid.CreateVersion7(at);
        return made;
    }
}
