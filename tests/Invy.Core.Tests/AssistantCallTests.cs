using Invy.Core.Managers;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>The calls the MCP tools use: where things are, changing part of an item, moving several, and types with made-up search names.</summary>
public class AssistantCallTests
{
    [Test]
    public async Task Where_is_says_which_containers_a_thing_is_in()
    {
        var space = await ItemSpace.MakeAsync();
        var garage = await space.BoxAsync("Garage");
        var f = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Box") with { Marking = "F", TypeId = space.Box.Id, ParentId = garage.Id }, default);
        await space.ThingAsync("Baseball", f);
        await space.ThingAsync("Baseball cards", quantity: 40m);

        var found = await space.Items.WhereIsAsync(space.Vera, space.SpaceId, "baseball", default);

        var sorted = found.Matches.OrderBy(match => match.Name).ToList();
        await Assert.That(found.Total).IsEqualTo(2);
        await Assert.That(sorted[0].Where).IsEqualTo("Garage › Box");
        await Assert.That(sorted[0].Amount).IsNull();
        await Assert.That(sorted[1].Where).IsEqualTo("the Inbox (no place yet)");
        await Assert.That(sorted[1].Amount).IsEqualTo("40");
        await Assert.That((await TestData.Caught<RuleViolationException>(() => space.Items.WhereIsAsync(space.Vera, space.SpaceId, " ", default)))!.Message)
            .Contains("what to look for");
    }

    [Test]
    public async Task Changing_part_of_an_item_keeps_the_rest_and_reads_fields_by_search_name()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with
        {
            TypeId = space.Tool.Id, Notes = "Charger in the drawer", Tags = ["garage"],
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement> { [space.Field("brand")] = ItemSpace.Json("Bosch") },
        }, default);

        var changed = await space.Items.ChangeItemAsync(space.Owen, space.SpaceId, drill.Id, new ItemChangesRequest(
            Fields: new Dictionary<string, string> { ["Price"] = "$129.99", ["power"] = "cordless", ["brand"] = "" }), default);

        await Assert.That(changed.Name).IsEqualTo("Drill");
        await Assert.That(changed.Notes).IsEqualTo("Charger in the drawer");
        await Assert.That(changed.Tags).IsEquivalentTo(["garage"]);
        await Assert.That(changed.Fields[space.Field("price")].GetDecimal()).IsEqualTo(129.99m);
        await Assert.That(changed.Fields[space.Field("power")].GetString()).IsEqualTo("Cordless");
        await Assert.That(changed.Fields.ContainsKey(space.Field("brand"))).IsFalse();
        await Assert.That(changed.Version).IsEqualTo(drill.Version + 1);
    }

    [Test]
    public async Task Unknown_fields_and_values_that_do_not_fit_are_refused_with_what_would()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with { TypeId = space.Tool.Id }, default);
        var ball = await space.ThingAsync("Baseball");

        var unknown = await TestData.Caught<RuleViolationException>(() => space.Items.ChangeItemAsync(space.Owen, space.SpaceId, drill.Id,
            new ItemChangesRequest(Fields: new Dictionary<string, string> { ["colour"] = "red" }), default));
        var wrong = await TestData.Caught<RuleViolationException>(() => space.Items.ChangeItemAsync(space.Owen, space.SpaceId, drill.Id,
            new ItemChangesRequest(Fields: new Dictionary<string, string> { ["power"] = "steam" }), default));
        var untyped = await TestData.Caught<RuleViolationException>(() => space.Items.ChangeItemAsync(space.Owen, space.SpaceId, ball.Id,
            new ItemChangesRequest(Fields: new Dictionary<string, string> { ["brand"] = "Rawlings" }), default));

        await Assert.That(unknown!.Message).Contains("brand, price, power, bought");
        await Assert.That(wrong!.Message).Contains("Hand, Cordless");
        await Assert.That(untyped!.Message).Contains("no type");
    }

    [Test]
    public async Task Changing_a_containers_name_keeps_it_holding_items()
    {
        var space = await ItemSpace.MakeAsync();
        var crate = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Crate") with { HoldsItems = true }, default);
        await space.ThingAsync("Glove", crate);

        var renamed = await space.Items.ChangeItemAsync(space.Owen, space.SpaceId, crate.Id, new ItemChangesRequest(Name: "Red crate", Marking: ""), default);

        await Assert.That(renamed.HoldsItems).IsTrue();
        await Assert.That(renamed.Name).IsEqualTo("Red crate");
    }

    [Test]
    public async Task Moving_several_moves_the_outermost_with_one_event_each_and_one_commit()
    {
        var space = await ItemSpace.MakeAsync();
        var f = await space.BoxAsync("Box F");
        var bag = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Bag") with { HoldsItems = true }, default);
        var ball = await space.ThingAsync("Baseball", bag);
        var glove = await space.ThingAsync("Glove");
        var commits = space.Core.Messages.Commits;

        var moved = await space.Items.MoveItemsAsync(space.Owen, space.SpaceId, new MoveItemsRequest([bag.Id, ball.Id, glove.Id], f.Id), default);

        await Assert.That(string.Join(", ", moved.Select(item => item.Name).Order())).IsEqualTo("Bag, Glove");
        await Assert.That(space.Core.Items.Named("Baseball").ParentId).IsEqualTo(bag.Id);
        await Assert.That(space.Core.Items.Named("Baseball").Path).StartsWith(space.Core.Items.Named("Box F").Path);
        await Assert.That(space.Core.Items.Events.Count(e => e.Kind == ItemEventKind.Moved)).IsEqualTo(2);
        await Assert.That(space.Core.Messages.Commits).IsEqualTo(commits + 1);
    }

    [Test]
    public async Task Moving_several_is_refused_whole_when_one_cannot_go()
    {
        var space = await ItemSpace.MakeAsync();
        var f = await space.BoxAsync("Box F");
        var glove = await space.ThingAsync("Glove");

        var intoItself = await TestData.Caught<RuleViolationException>(() =>
            space.Items.MoveItemsAsync(space.Owen, space.SpaceId, new MoveItemsRequest([glove.Id, f.Id], f.Id), default));
        var none = await TestData.Caught<RuleViolationException>(() =>
            space.Items.MoveItemsAsync(space.Owen, space.SpaceId, new MoveItemsRequest([], f.Id), default));
        var viewer = await TestData.Caught<NotAllowedException>(() =>
            space.Items.MoveItemsAsync(space.Vera, space.SpaceId, new MoveItemsRequest([glove.Id], f.Id), default));

        await Assert.That(intoItself).IsNotNull();
        await Assert.That(none!.Message).Contains($"1 to {ItemManager.MaxMovedAtOnce}");
        await Assert.That(viewer).IsNotNull();
    }

    [Test]
    public async Task A_type_gets_search_names_from_field_labels_when_none_are_given()
    {
        var space = await ItemSpace.MakeAsync();

        var lego = await space.Core.TypeManager().CreateTypeAsync(space.Owen, space.SpaceId, new ItemTypeRequest("Lego set", null, null, false,
        [
            new FieldDefinition(null, "", "Set number", FieldKind.Text, null, [], false, true),
            new FieldDefinition(null, "", "Pieces", FieldKind.Number, null, [], false, true),
        ], 0), default);

        await Assert.That(string.Join(", ", lego.Fields.Select(field => field.Key))).IsEqualTo("set_number, pieces");
    }
}
