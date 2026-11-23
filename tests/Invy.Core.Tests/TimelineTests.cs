using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

public class TimelineTests
{
    private static readonly TimelineEngine Engine = new();

    private static readonly Guid Shed = Guid.NewGuid(), Garage = Guid.NewGuid(), Abc = Guid.NewGuid(), F = Guid.NewGuid(), Ball = Guid.NewGuid();

    private static DateTimeOffset May(int day) => new(2026, 5, day, 12, 0, 0, TimeSpan.Zero);

    private static ItemEvent Move(Guid item, DateTimeOffset at, Guid? to, ItemEventKind kind = ItemEventKind.Moved) =>
        new() { ItemId = item, At = at, ToParent = to, Kind = kind };

    /// <summary>
    /// 1 May: the shed and the garage. 2 May: box ABC in the shed, box F in ABC. 3 May: the baseball in F.
    /// 10 May: F to the garage. 20 May: the baseball out of F onto the garage floor.
    /// </summary>
    private static List<ItemEvent> History() =>
    [
        Move(Shed, May(1), null, ItemEventKind.Created),
        Move(Garage, May(1), null, ItemEventKind.Created),
        Move(Abc, May(2), Shed, ItemEventKind.Created),
        Move(F, May(2), Abc, ItemEventKind.Created),
        Move(Ball, May(3), F, ItemEventKind.Created),
        Move(F, May(10), Garage),
        Move(Ball, May(20), Garage),
    ];

    private static string Names(IReadOnlyList<Guid>? chain)
    {
        var names = new Dictionary<Guid, string> { [Shed] = "Shed", [Garage] = "Garage", [Abc] = "Box ABC", [F] = "Box F" };
        return chain is null ? "(not yet)" : string.Join(" › ", chain.Select(id => names[id]));
    }

    [Test]
    public async Task Where_it_has_been_counts_its_containers_moves()
    {
        var places = Engine.Places(Ball, History());

        await Assert.That(string.Join(" | ", places.Select(stay => $"{stay.From:dd} {Names(stay.Chain)}")))
            .IsEqualTo("03 Shed › Box ABC › Box F | 10 Garage › Box F | 20 Garage");
        await Assert.That(places[0].Until).IsEqualTo(May(10));
        await Assert.That(places[^1].Until).IsNull();
    }

    [Test]
    [Arguments(2, "(not yet)")]
    [Arguments(3, "Shed › Box ABC › Box F")]
    [Arguments(9, "Shed › Box ABC › Box F")]
    [Arguments(10, "Garage › Box F")]
    [Arguments(15, "Garage › Box F")]
    [Arguments(25, "Garage")]
    public async Task Location_on_a_past_date(int day, string expected)
    {
        var places = Engine.Places(Ball, History());

        await Assert.That(Names(Engine.LocationAt(places, May(day)))).IsEqualTo(expected);
    }

    [Test]
    public async Task Moves_of_a_container_before_the_item_went_in_do_not_count()
    {
        // F was moved within the shed before the baseball existed.
        var history = History();
        history.Add(Move(F, May(2).AddHours(1), Shed));

        var places = Engine.Places(Ball, history);

        await Assert.That(Names(places[0].Chain)).IsEqualTo("Shed › Box F");
    }

    [Test]
    public async Task The_item_page_shows_where_it_has_been_and_what_went_in_and_out()
    {
        var space = await ItemSpace.MakeAsync();
        var clock = space.Core.Clock;
        var shed = await space.BoxAsync("Shed");
        var garage = await space.BoxAsync("Garage");
        var abc = await space.BoxAsync("Box ABC", shed);
        var f = await space.BoxAsync("Box F", abc);
        clock.Now = clock.Now.AddHours(1);
        var ball = await space.ThingAsync("Baseball", f);
        clock.Now = clock.Now.AddDays(3);
        await space.Items.MoveItemAsync(space.Owen, space.SpaceId, f.Id, new MoveItemRequest(garage.Id), default);

        var history = await space.Items.GetHistoryAsync(space.Vera, space.SpaceId, ball.Id, default);
        await Assert.That(string.Join(" | ", history.Places.Select(stay => string.Join(" › ", stay.Chain.Select(c => c.Name)))))
            .IsEqualTo("Garage › Box F | Shed › Box ABC › Box F");
        await Assert.That(history.Events.Single().Actor).IsEqualTo("Owen");

        var box = await space.Items.GetHistoryAsync(space.Vera, space.SpaceId, f.Id, default);
        await Assert.That(string.Join(", ", box.Events.Select(e => $"{e.Kind} {e.Item.Name}")))
            .IsEqualTo("Moved Box F, Created Baseball, Created Box F");
        await Assert.That(box.Events[0].From?.Name).IsEqualTo("Box ABC");
        await Assert.That(box.Events[0].To?.Name).IsEqualTo("Garage");
    }

    [Test]
    public async Task Activity_is_the_space_feed_newest_first_a_page_at_a_time()
    {
        var space = await ItemSpace.MakeAsync();
        var bin = await space.BoxAsync("Bin");
        for (var i = 0; i < 6; i++)
        {
            space.Core.Clock.Now = space.Core.Clock.Now.AddMinutes(1);
            await space.Items.QuickAddAsync(space.Owen, space.SpaceId,
                new QuickAddRequest(string.Join(", ", Enumerable.Range(i * 10, 10).Select(n => $"Part {n}")), bin.Id), default);
        }

        var first = await space.Items.ListActivityAsync(space.Vera, space.SpaceId, null, default);
        var second = await space.Items.ListActivityAsync(space.Vera, space.SpaceId, first.Next, default);

        await Assert.That(first.Events.Count).IsEqualTo(50);
        await Assert.That(first.Events[0].At).IsGreaterThanOrEqualTo(first.Events[^1].At);
        await Assert.That(second.Events.Count).IsEqualTo(11);
        await Assert.That(second.Events[^1].Item.Name).IsEqualTo("Bin");
        await Assert.That(first.Events.Select(e => e.Id).Intersect(second.Events.Select(e => e.Id))).IsEmpty();
        await Assert.That(second.Next).IsNull();
    }
}
