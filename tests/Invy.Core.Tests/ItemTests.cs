using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

public class ItemTests
{
    [Test]
    public async Task Adding_an_item_places_it_and_writes_a_created_event()
    {
        var space = await ItemSpace.MakeAsync();
        var shed = await space.BoxAsync("Shed");
        var abc = await space.BoxAsync("Box ABC", shed);

        var stored = space.Core.Items.Named("Box ABC");
        await Assert.That(stored.Path).IsEqualTo($"{ItemPaths.Label(shed.Id)}.{ItemPaths.Label(abc.Id)}");
        await Assert.That(stored.Depth).IsEqualTo(1);
        await Assert.That(abc.HoldsItems).IsTrue();
        var created = space.Core.Items.Events.Single(e => e.ItemId == abc.Id);
        await Assert.That(created.Kind).IsEqualTo(ItemEventKind.Created);
        await Assert.That(created.ToParent).IsEqualTo(shed.Id);
        await Assert.That(created.ActorId).IsEqualTo(space.Owen);
    }

    [Test]
    public async Task Moving_a_box_moves_everything_inside_with_one_event()
    {
        // Shed › Box ABC › Box F › Baseball, then Box F goes to the garage.
        var space = await ItemSpace.MakeAsync();
        var shed = await space.BoxAsync("Shed");
        var garage = await space.BoxAsync("Garage");
        var abc = await space.BoxAsync("Box ABC", shed);
        var f = await space.BoxAsync("Box F", abc);
        var baseball = await space.ThingAsync("Baseball", f);
        var eventsBefore = space.Core.Items.Events.Count;

        await space.Items.MoveItemAsync(space.Owen, space.SpaceId, f.Id, new MoveItemRequest(garage.Id), default);

        var newEvents = space.Core.Items.Events.Skip(eventsBefore).ToList();
        await Assert.That(newEvents.Count).IsEqualTo(1);
        await Assert.That(newEvents[0].ItemId).IsEqualTo(f.Id);
        await Assert.That(newEvents[0].FromParent).IsEqualTo(abc.Id);
        await Assert.That(newEvents[0].ToParent).IsEqualTo(garage.Id);
        await Assert.That(space.Core.Items.SubtreeMoves).IsEqualTo(1);

        var ball = space.Core.Items.Named("Baseball");
        await Assert.That(ball.Path).IsEqualTo(string.Join(".", new[] { garage.Id, f.Id, baseball.Id }.Select(ItemPaths.Label)));
        await Assert.That(ball.Depth).IsEqualTo(2);
        var page = await space.Items.GetItemAsync(space.Vera, space.SpaceId, baseball.Id, default);
        await Assert.That(string.Join(" › ", page.Chain.Select(c => c.Name))).IsEqualTo("Garage › Box F");
    }

    [Test]
    public async Task An_item_cannot_go_into_itself_or_anything_inside_it()
    {
        var space = await ItemSpace.MakeAsync();
        var abc = await space.BoxAsync("Box ABC");
        var f = await space.BoxAsync("Box F", abc);

        var intoItself = await TestData.Caught<RuleViolationException>(
            () => space.Items.MoveItemAsync(space.Owen, space.SpaceId, abc.Id, new MoveItemRequest(abc.Id), default));
        var intoChild = await TestData.Caught<RuleViolationException>(
            () => space.Items.MoveItemAsync(space.Owen, space.SpaceId, abc.Id, new MoveItemRequest(f.Id), default));

        await Assert.That(intoItself?.Message).IsEqualTo("Box ABC can't go inside itself. Pick another place.");
        await Assert.That(intoChild?.Message).IsEqualTo("Box F is inside Box ABC, so Box ABC can't go into it. Move Box F out first.");
        await Assert.That(space.Core.Items.SubtreeMoves).IsEqualTo(0);
    }

    [Test]
    public async Task Only_containers_take_items()
    {
        var space = await ItemSpace.MakeAsync();
        var glove = await space.ThingAsync("Glove");

        var refused = await TestData.Caught<RuleViolationException>(() => space.ThingAsync("Ball", glove));

        await Assert.That(refused?.Message).Contains("Glove doesn't hold items");
    }

    [Test]
    public async Task Sortable_fields_are_copied_as_typed_values()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with
        {
            TypeId = space.Tool.Id,
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement>
            {
                [space.Field("brand")] = ItemSpace.Json("DeWalt"),
                [space.Field("price")] = ItemSpace.Json(129.5m),
                [space.Field("power")] = ItemSpace.Json("Cordless"),
                [space.Field("bought")] = ItemSpace.Json("2025-06-01"),
            },
        }, default);

        var values = space.Core.Items.Values.Where(v => v.ItemId == drill.Id).ToDictionary(v => v.FieldId);
        await Assert.That(values.Count).IsEqualTo(3);
        await Assert.That(values[space.Field("brand")].Text).IsEqualTo("dewalt");
        await Assert.That(values[space.Field("price")].Num).IsEqualTo(129.5m);
        await Assert.That(values[space.Field("bought")].Date).IsEqualTo(new DateOnly(2025, 6, 1));

        var edit = ItemSpace.Edit(drill);
        var fields = edit.Fields!.ToDictionary();
        fields.Remove(space.Field("price"));
        await space.Items.UpdateItemAsync(space.Owen, space.SpaceId, drill.Id, edit with { Fields = fields }, default);
        await Assert.That(space.Core.Items.Values.Count(v => v.ItemId == drill.Id)).IsEqualTo(2);
    }

    [Test]
    public async Task Changing_an_item_lists_what_changed_and_refuses_an_old_version()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with
        {
            TypeId = space.Tool.Id,
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement> { [space.Field("brand")] = ItemSpace.Json("Makita") },
        }, default);

        var edit = ItemSpace.Edit(drill);
        var saved = await space.Items.UpdateItemAsync(space.Owen, space.SpaceId, drill.Id, edit with
        {
            Name = "Hammer drill",
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement> { [space.Field("brand")] = ItemSpace.Json("DeWalt") },
            Tags = ["garage"],
        }, default);

        var changed = space.Core.Items.Events.Single(e => e.Kind == ItemEventKind.Changed);
        await Assert.That(string.Join("; ", changed.Changes.Select(c => $"{c.What}: {c.Before} → {c.After}")))
            .IsEqualTo("Name: Drill → Hammer drill; Tags:  → garage; Brand: Makita → DeWalt");
        await Assert.That(saved.Version).IsEqualTo(2);

        var stale = await TestData.Caught<ConflictException>(
            () => space.Items.UpdateItemAsync(space.Owen, space.SpaceId, drill.Id, edit with { Name = "Old" }, default));
        await Assert.That(stale).IsNotNull();
    }

    [Test]
    public async Task Values_of_a_field_the_type_no_longer_has_are_kept_only_unchanged()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with
        {
            TypeId = space.Tool.Id,
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement> { [space.Field("brand")] = ItemSpace.Json("Makita") },
        }, default);

        // Moving it to the Box type keeps the brand as another field.
        var asBox = await space.Items.UpdateItemAsync(space.Owen, space.SpaceId, drill.Id, ItemSpace.Edit(drill) with { TypeId = space.Box.Id }, default);
        await Assert.That(asBox.Fields[space.Field("brand")].GetString()).IsEqualTo("Makita");

        var changedOther = ItemSpace.Edit(asBox) with
        {
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement> { [space.Field("brand")] = ItemSpace.Json("Bosch") },
        };
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.UpdateItemAsync(space.Owen, space.SpaceId, drill.Id, changedOther, default))).IsNotNull();
    }

    [Test]
    [Arguments("power", "\"Petrol\"", "Power needs one of its choices: Hand, Cordless.")]
    [Arguments("price", "\"cheap\"", "Price needs a number.")]
    [Arguments("bought", "\"1 June\"", "Bought needs a date.")]
    [Arguments("brand", "42", "Brand needs text of up to 2,000 characters.")]
    public async Task Field_values_must_suit_their_kind(string key, string json, string problem)
    {
        var space = await ItemSpace.MakeAsync();

        var refused = await TestData.Caught<RuleViolationException>(() => space.Items.CreateItemAsync(space.Owen, space.SpaceId,
            ItemSpace.Request("Drill") with
            {
                TypeId = space.Tool.Id,
                Fields = new Dictionary<Guid, System.Text.Json.JsonElement> { [space.Field(key)] = System.Text.Json.JsonDocument.Parse(json).RootElement },
            }, default));

        await Assert.That(refused?.Message).IsEqualTo(problem);
    }

    [Test]
    public async Task Counted_items_go_up_and_down_but_not_below_zero()
    {
        var space = await ItemSpace.MakeAsync();
        var screws = await space.ThingAsync("Screws", quantity: 200);
        var ball = await space.ThingAsync("Baseball");

        var fewer = await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-15), default);

        await Assert.That(fewer.Quantity).IsEqualTo(185m);
        await Assert.That(space.Core.Items.Events.Single(e => e.Kind == ItemEventKind.QuantityChanged).QtyDelta).IsEqualTo(-15m);
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-200), default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, ball.Id, new AdjustQuantityRequest(1), default))).IsNotNull();
    }

    [Test]
    public async Task Disposing_needs_an_empty_container_and_restoring_brings_it_back()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        var ball = await space.ThingAsync("Baseball", box);

        var refused = await TestData.Caught<RuleViolationException>(() => space.Items.DisposeItemAsync(space.Owen, space.SpaceId, box.Id, default));
        await Assert.That(refused?.Message).IsEqualTo("Box F still has 1 item inside. Move them out first, then dispose of it.");

        await space.Items.DisposeItemAsync(space.Owen, space.SpaceId, ball.Id, default);
        await Assert.That((await space.Items.ListContentsAsync(space.Owen, space.SpaceId, box.Id, null, default)).Items).IsEmpty();

        var back = await space.Items.RestoreItemAsync(space.Owen, space.SpaceId, ball.Id, default);
        await Assert.That(back.Status).IsEqualTo(ItemStatus.Active);
        await Assert.That(string.Join(", ", space.Core.Items.Events.Where(e => e.ItemId == ball.Id).Select(e => e.Kind)))
            .IsEqualTo("Created, Disposed, Restored");
    }

    [Test]
    public async Task Things_without_a_place_or_type_wait_in_the_inbox()
    {
        var space = await ItemSpace.MakeAsync();
        var shed = await space.BoxAsync("Shed");
        await space.ThingAsync("Glove");
        await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Saw") with { TypeId = space.Tool.Id }, default);

        var top = await space.Items.ListContentsAsync(space.Vera, space.SpaceId, null, null, default);
        var inbox = await space.Items.ListInboxAsync(space.Vera, space.SpaceId, null, default);

        await Assert.That(string.Join(", ", top.Items.Select(i => i.Name))).IsEqualTo("Saw, Shed");
        await Assert.That(string.Join(", ", inbox.Items.Select(i => i.Name))).IsEqualTo("Glove");
        await Assert.That(top.Items.Single(i => i.Id == shed.Id).Contents).IsEqualTo(0);
    }

    [Test]
    public async Task Contents_come_a_page_at_a_time()
    {
        var space = await ItemSpace.MakeAsync();
        var bin = await space.BoxAsync("Bin");
        foreach (var range in new[] { (1, 60), (61, 45) })
        {
            var names = string.Join(", ", Enumerable.Range(range.Item1, range.Item2).Select(i => $"Part {i:000}"));
            await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new QuickAddRequest(names, bin.Id), default);
        }

        var first = await space.Items.ListContentsAsync(space.Owen, space.SpaceId, bin.Id, null, default);
        var second = await space.Items.ListContentsAsync(space.Owen, space.SpaceId, bin.Id, first.Next, default);

        await Assert.That(first.Items.Count).IsEqualTo(100);
        await Assert.That(second.Items.Count).IsEqualTo(5);
        await Assert.That(second.Items[0].Name).IsEqualTo("Part 101");
        await Assert.That(second.Next).IsNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.ListContentsAsync(space.Owen, space.SpaceId, bin.Id, "not-a-cursor!", default))).IsNotNull();
    }

    [Test]
    public async Task A_viewer_can_look_but_not_change_items()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");

        await Assert.That((await space.Items.GetItemAsync(space.Vera, space.SpaceId, box.Id, default)).Item.Name).IsEqualTo("Box F");
        await Assert.That(await TestData.Caught<NotAllowedException>(
            () => space.Items.CreateItemAsync(space.Vera, space.SpaceId, ItemSpace.Request("Ball"), default))).IsNotNull();
        await Assert.That(await TestData.Caught<NotAllowedException>(
            () => space.Items.MoveItemAsync(space.Vera, space.SpaceId, box.Id, new MoveItemRequest(null), default))).IsNotNull();
    }

    [Test]
    public async Task Quick_add_reads_names_and_amounts_into_a_container()
    {
        var space = await ItemSpace.MakeAsync();
        var f = await space.BoxAsync("Box F");

        var made = await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new QuickAddRequest("baseball, glove, 2 bats", f.Id), default);

        await Assert.That(string.Join(", ", made.Select(i => $"{i.Name}:{i.Quantity}"))).IsEqualTo("Baseball:, Glove:, Bats:2");
        await Assert.That(made.All(i => i.ParentId == f.Id)).IsTrue();
        await Assert.That(space.Core.Items.Events.Count(e => e.Kind == ItemEventKind.Created && e.ToParent == f.Id)).IsEqualTo(3);
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.QuickAddAsync(space.Owen, space.SpaceId, new QuickAddRequest(" , ", null), default))).IsNotNull();
    }
}
