using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

public class SearchTests
{
    /// <summary>Shed › Box ABC › Box F › Baseball, a garage, and a DeWalt drill worth 129.50 in the garage.</summary>
    private static async Task<(ItemSpace Space, ItemResponse Garage, ItemResponse F, ItemResponse Ball)> ShedAsync()
    {
        var space = await ItemSpace.MakeAsync();
        var shed = await space.BoxAsync("Shed");
        var garage = await space.BoxAsync("Garage");
        var abc = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Box") with { Marking = "ABC", TypeId = space.Box.Id, ParentId = shed.Id }, default);
        var f = await space.BoxAsync("Box F", abc);
        var ball = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Baseball") with { ParentId = f.Id, Tags = ["Sports"] }, default);
        await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with
        {
            TypeId = space.Tool.Id,
            ParentId = garage.Id,
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement>
            {
                [space.Field("brand")] = ItemSpace.Json("DeWalt"),
                [space.Field("price")] = ItemSpace.Json(129.5m),
                [space.Field("power")] = ItemSpace.Json("Cordless"),
            },
        }, default);
        return (space, garage, f, ball);
    }

    private static async Task<string> FindAsync(ItemSpace space, string text, Guid? userId = null) =>
        string.Join(", ", (await space.Items.SearchAsync(userId ?? space.Owen, space.SpaceId, text, "name", null, default)).Results.Select(r => r.Item.Name));

    [Test]
    public async Task Find_the_baseball_after_its_box_moved_and_see_where_it_is()
    {
        var (space, garage, f, _) = await ShedAsync();
        await space.Items.MoveItemAsync(space.Owen, space.SpaceId, f.Id, new MoveItemRequest(garage.Id), default);

        var found = await space.Items.SearchAsync(space.Vera, space.SpaceId, "baseball", null, null, default);

        await Assert.That(found.Total).IsEqualTo(1);
        await Assert.That(string.Join(" › ", found.Results.Single().Chain.Select(c => c.Name))).IsEqualTo("Garage › Box F");
    }

    [Test]
    public async Task Search_terms_narrow_the_results()
    {
        var (space, _, _, _) = await ShedAsync();

        await Assert.That(await FindAsync(space, "in:shed")).IsEqualTo("Baseball, Box, Box F");
        await Assert.That(await FindAsync(space, "in:abc")).IsEqualTo("Baseball, Box F");
        await Assert.That(await FindAsync(space, "type:tool brand:dewalt")).IsEqualTo("Drill");
        await Assert.That(await FindAsync(space, "power:cordless price>100")).IsEqualTo("Drill");
        await Assert.That(await FindAsync(space, "price<100")).IsEqualTo(string.Empty);
        await Assert.That(await FindAsync(space, "tag:sports")).IsEqualTo("Baseball");
        await Assert.That(await FindAsync(space, "is:container -in:shed")).IsEqualTo("Garage, Shed");
        await Assert.That(await FindAsync(space, "drill OR baseball")).IsEqualTo("Baseball, Drill");
    }

    [Test]
    public async Task Disposed_things_are_found_only_when_asked_for()
    {
        var (space, _, _, ball) = await ShedAsync();
        await space.Items.DisposeItemAsync(space.Owen, space.SpaceId, ball.Id, default);

        await Assert.That(await FindAsync(space, "baseball")).IsEqualTo(string.Empty);
        await Assert.That(await FindAsync(space, "baseball is:disposed")).IsEqualTo("Baseball");
    }

    [Test]
    public async Task Results_come_fifty_at_a_time_with_the_total_on_the_first_page()
    {
        var space = await ItemSpace.MakeAsync();
        var bin = await space.BoxAsync("Bin");
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new QuickAddRequest(string.Join(", ", Enumerable.Range(1, 60).Select(i => $"Part {i:00}")), bin.Id), default);

        var first = await space.Items.SearchAsync(space.Owen, space.SpaceId, "part", "name", null, default);
        var second = await space.Items.SearchAsync(space.Owen, space.SpaceId, "part", "name", first.Next, default);

        await Assert.That(first.Results.Count).IsEqualTo(50);
        await Assert.That(first.Total).IsEqualTo(60);
        await Assert.That(second.Results.Count).IsEqualTo(10);
        await Assert.That(second.Results[0].Item.Name).IsEqualTo("Part 51");
        await Assert.That(second.Total).IsNull();
        await Assert.That(second.Next).IsNull();
    }

    [Test]
    public async Task A_search_that_cannot_be_read_says_how_to_write_it()
    {
        var (space, _, _, _) = await ShedAsync();

        var refused = await TestData.Caught<RuleViolationException>(() => space.Items.SearchAsync(space.Owen, space.SpaceId, "type:car", null, null, default));

        await Assert.That(refused?.Message).IsEqualTo("There is no type called car. The types are Box, Tool.");
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.SearchAsync(space.Owen, space.SpaceId, "x", null, "garbage!", default))).IsNotNull();
    }

    [Test]
    public async Task Containers_keep_counts_and_value_totals_of_everything_inside()
    {
        var (space, garage, f, _) = await ShedAsync();
        await RunRollupsAsync(space);
        await Assert.That(space.Core.Items.Named("Shed").ContentCount).IsEqualTo(3);
        await Assert.That(space.Core.Items.Named("Garage").ContentValue).IsEqualTo(129.5m);

        // Moving box F (with the baseball) to the garage recounts both sides.
        space.Core.Messages.Published.Clear();
        await space.Items.MoveItemAsync(space.Owen, space.SpaceId, f.Id, new MoveItemRequest(garage.Id), default);
        var changed = space.Core.Messages.Published.OfType<ItemsChanged>().Single();
        await Assert.That(changed.ContainerIds.Count).IsEqualTo(3);
        await RunRollupsAsync(space);

        await Assert.That(space.Core.Items.Named("Shed").ContentCount).IsEqualTo(1);
        await Assert.That(space.Core.Items.Named("Garage").ContentCount).IsEqualTo(3);
        var top = await space.Items.ListContentsAsync(space.Owen, space.SpaceId, null, null, default);
        await Assert.That(top.Items.Single(i => i.Name == "Garage").Contents).IsEqualTo(3);
    }

    [Test]
    public async Task An_items_value_is_its_first_money_field_times_its_amount()
    {
        var space = await ItemSpace.MakeAsync();
        var bits = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill bits") with
        {
            TypeId = space.Tool.Id,
            Quantity = 4,
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement> { [space.Field("price")] = ItemSpace.Json(2.5m) },
        }, default);

        await Assert.That(bits.Value).IsEqualTo(10m);
        var fewer = await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, bits.Id, new AdjustQuantityRequest(-2), default);
        await Assert.That(fewer.Value).IsEqualTo(5m);
    }

    /// <summary>Does what the rollups queue does with each ItemsChanged published so far.</summary>
    private static async Task RunRollupsAsync(ItemSpace space)
    {
        foreach (var message in space.Core.Messages.Published.OfType<ItemsChanged>().ToList())
        {
            await space.Items.UpdateRollupsAsync(message, default);
        }
    }
}
