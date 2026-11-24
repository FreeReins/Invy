using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

public class SeenAndAuditTests
{
    [Test]
    public async Task Opening_a_box_marks_it_and_what_is_directly_inside_with_one_event()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        var inner = await space.BoxAsync("Box G", box);
        await space.ThingAsync("Baseball", box);
        await space.ThingAsync("Marble", inner);
        var eventsBefore = space.Core.Items.Events.Count;

        var seen = await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, box.Id, default);

        var now = space.Core.Clock.Now;
        await Assert.That(seen.LastSeenAt).IsEqualTo(now);
        await Assert.That(space.Core.Items.Named("Baseball").LastSeenAt).IsEqualTo(now);
        await Assert.That(space.Core.Items.Named("Box G").LastSeenAt).IsEqualTo(now);
        await Assert.That(space.Core.Items.Named("Marble").LastSeenAt).IsNull();
        var newEvents = space.Core.Items.Events.Skip(eventsBefore).ToList();
        await Assert.That(newEvents.Count).IsEqualTo(1);
        await Assert.That(newEvents[0].Kind).IsEqualTo(ItemEventKind.Seen);
        await Assert.That(newEvents[0].ItemId).IsEqualTo(box.Id);
    }

    [Test]
    public async Task Seeing_a_thing_announces_it_so_open_views_load_again()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        var ball = await space.ThingAsync("Baseball", box);
        space.Core.Messages.Published.Clear();

        await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, ball.Id, default);

        var changed = space.Core.Messages.Published.OfType<ItemsChanged>().Single();
        await Assert.That(string.Join(", ", changed.ContainerIds)).IsEqualTo(box.Id.ToString());
    }

    [Test]
    public async Task Opening_it_again_soon_writes_nothing_more()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, box.Id, default);
        var eventsBefore = space.Core.Items.Events.Count;

        space.Core.Clock.Now += TimeSpan.FromMinutes(5);
        await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, box.Id, default);
        var afterFive = space.Core.Items.Events.Count;
        space.Core.Clock.Now += TimeSpan.FromMinutes(10);
        await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, box.Id, default);

        await Assert.That(afterFive).IsEqualTo(eventsBefore);
        await Assert.That(space.Core.Items.Events.Count).IsEqualTo(eventsBefore + 1);
    }

    [Test]
    public async Task Viewers_cannot_mark_things_seen()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");

        var refused = await TestData.Caught<NotAllowedException>(() => space.Items.MarkSeenAsync(space.Vera, space.SpaceId, box.Id, default));

        await Assert.That(refused).IsNotNull();
    }

    [Test]
    public async Task An_audit_marks_what_was_not_ticked_missing_and_lists_the_differences()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        var shelf = await space.BoxAsync("Shelf");
        var ball = await space.ThingAsync("Baseball", box);
        var glove = await space.ThingAsync("Glove", box);
        await space.ThingAsync("Bat", box);
        var drill = await space.ThingAsync("Drill", shelf);

        var audit = await space.Items.AuditAsync(space.Owen, space.SpaceId, box.Id, new AuditRequest([ball.Id, drill.Id]), default);

        await Assert.That(audit.Seen).IsEqualTo(2);
        await Assert.That(string.Join(", ", audit.Missing.Select(item => item.Name).Order())).IsEqualTo("Bat, Glove");
        await Assert.That(audit.Found.Count).IsEqualTo(0);
        await Assert.That(string.Join(", ", audit.Elsewhere.Select(item => item.Name))).IsEqualTo("Drill");
        await Assert.That(space.Core.Items.Named("Glove").Status).IsEqualTo(ItemStatus.Missing);
        await Assert.That(space.Core.Items.Named("Baseball").LastSeenAt).IsEqualTo(space.Core.Clock.Now);
        await Assert.That(space.Core.Items.Named("Drill").ParentId).IsEqualTo(shelf.Id);
        await Assert.That(space.Core.Items.Events.Count(e => e.ItemId == glove.Id && e.Kind == ItemEventKind.Missing)).IsEqualTo(1);
    }

    [Test]
    public async Task Ticking_a_missing_item_in_a_later_audit_finds_it()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        var ball = await space.ThingAsync("Baseball", box);
        await space.Items.AuditAsync(space.Owen, space.SpaceId, box.Id, new AuditRequest([]), default);

        var second = await space.Items.AuditAsync(space.Owen, space.SpaceId, box.Id, new AuditRequest([ball.Id]), default);

        await Assert.That(string.Join(", ", second.Found.Select(item => item.Name))).IsEqualTo("Baseball");
        await Assert.That(second.Missing.Count).IsEqualTo(0);
        await Assert.That(space.Core.Items.Named("Baseball").Status).IsEqualTo(ItemStatus.Active);
        await Assert.That(space.Core.Items.Events.Count(e => e.ItemId == ball.Id && e.Kind == ItemEventKind.Found)).IsEqualTo(1);
    }

    [Test]
    public async Task An_item_already_missing_is_listed_again_without_another_event()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        var ball = await space.ThingAsync("Baseball", box);
        await space.Items.AuditAsync(space.Owen, space.SpaceId, box.Id, new AuditRequest([]), default);

        var second = await space.Items.AuditAsync(space.Owen, space.SpaceId, box.Id, new AuditRequest([]), default);

        await Assert.That(string.Join(", ", second.Missing.Select(item => item.Name))).IsEqualTo("Baseball");
        await Assert.That(space.Core.Items.Events.Count(e => e.ItemId == ball.Id && e.Kind == ItemEventKind.Missing)).IsEqualTo(1);
    }

    [Test]
    public async Task Only_containers_can_be_audited()
    {
        var space = await ItemSpace.MakeAsync();
        var glove = await space.ThingAsync("Glove");

        var refused = await TestData.Caught<RuleViolationException>(
            () => space.Items.AuditAsync(space.Owen, space.SpaceId, glove.Id, new AuditRequest([]), default));

        await Assert.That(refused?.Message).Contains("Glove doesn't hold items");
    }
}
