using System.Text.Json;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>A space's calendar feed: its secret address, and the reminder and loan dates in it.</summary>
public class CalendarTests
{
    private static readonly Uri Site = new("https://invy.example/");

    [Test]
    public async Task Owners_turn_the_feed_on_and_its_address_holds_the_secret()
    {
        var space = await ItemSpace.MakeAsync();
        var spaces = space.Core.SpaceManager();

        var off = await spaces.GetCalendarFeedAsync(space.Vera, space.SpaceId, Site, default);
        var on = await spaces.StartCalendarFeedAsync(space.Owen, space.SpaceId, Site, default);

        await Assert.That(off.Url).IsNull();
        await Assert.That(on.Url!).StartsWith($"https://invy.example/api/spaces/{space.SpaceId}/calendar/");
        await Assert.That(await TestData.Caught<NotAllowedException>(() => spaces.StartCalendarFeedAsync(space.Vera, space.SpaceId, Site, default))).IsNotNull();
    }

    [Test]
    public async Task The_feed_has_reminder_dates_with_alarms_and_loans()
    {
        var space = await ItemSpace.MakeAsync();
        var spaces = space.Core.SpaceManager();
        var type = await space.Core.TypeManager().CreateTypeAsync(space.Owen, space.SpaceId, new ItemTypeRequest("Appliance", null, null, false,
            [new FieldDefinition(null, "warranty", "Warranty until", FieldKind.Date, null, [], false, false, Remind: 30)], 0), default);
        var warranty = type.Fields.Single().Id!.Value;
        await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Fridge, big") with
        {
            TypeId = type.Id, Fields = new Dictionary<Guid, JsonElement> { [warranty] = ItemSpace.Json("2027-01-05") },
        }, default);
        var drill = await space.ThingAsync("Drill");
        await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", TestData.Sunday.AddDays(3)), default);
        var url = (await spaces.StartCalendarFeedAsync(space.Owen, space.SpaceId, Site, default)).Url!;
        var token = url[(url.LastIndexOf('/') + 1)..^4];

        var feed = await spaces.GetCalendarAsync(space.SpaceId, token, Site, default);

        await Assert.That(feed).Contains("SUMMARY:Fridge\\, big: Warranty until\r\n");
        await Assert.That(feed).Contains("DTSTART;VALUE=DATE:20270105\r\n");
        await Assert.That(feed).Contains("TRIGGER:-P30D\r\n");
        await Assert.That(feed).Contains("SUMMARY:Drill due back from Sam\r\n");
        await Assert.That(feed).Contains("DTSTART;VALUE=DATE:20261111\r\n");
    }

    [Test]
    public async Task A_wrong_or_replaced_secret_finds_nothing()
    {
        var space = await ItemSpace.MakeAsync();
        var spaces = space.Core.SpaceManager();
        var first = (await spaces.StartCalendarFeedAsync(space.Owen, space.SpaceId, Site, default)).Url!;
        await spaces.StartCalendarFeedAsync(space.Owen, space.SpaceId, Site, default);
        var oldToken = first[(first.LastIndexOf('/') + 1)..^4];

        await Assert.That(await TestData.Caught<NotFoundException>(() => spaces.GetCalendarAsync(space.SpaceId, oldToken, Site, default))).IsNotNull();
        await Assert.That(await TestData.Caught<NotFoundException>(() => spaces.GetCalendarAsync(space.SpaceId, "nope", Site, default))).IsNotNull();
    }
}
