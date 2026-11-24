using System.Security.Claims;
using Invy.Api.Mcp;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol;

namespace Invy.Core.Tests;

/// <summary>The MCP tools as an assistant uses them: signed in with a token, naming a space or not.</summary>
public class McpToolTests
{
    [Test]
    public async Task An_assistant_finds_the_baseball_and_puts_the_glove_in_its_box()
    {
        var space = await ItemSpace.MakeAsync();
        var (items, _) = Tools(space, space.Owen);
        var garage = await space.BoxAsync("Garage");
        var f = await space.BoxAsync("Box F", garage);
        await items.AddItems("baseball", f.Id);
        await items.AddItems("glove, 2 bats");

        var baseball = (await items.WhereIs("baseball")).Matches.Single();
        var glove = (await items.WhereIs("glove")).Matches.Single();
        var moved = await items.MoveItems([glove.Id], f.Id);

        await Assert.That(baseball.Where).IsEqualTo("Garage › Box F");
        await Assert.That(glove.Where).IsEqualTo("the Inbox (no place yet)");
        await Assert.That(moved.Single().ParentId).IsEqualTo(f.Id);
        await Assert.That((await items.WhereIs("glove")).Matches.Single().Where).IsEqualTo("Garage › Box F");
        await Assert.That(string.Join(", ", (await items.ListContents(f.Id)).Items.Select(item => item.Name).Order())).IsEqualTo("Baseball, Glove");
    }

    [Test]
    public async Task Tools_honour_space_roles_and_explain_refusals()
    {
        var space = await ItemSpace.MakeAsync();
        var f = await space.BoxAsync("Box F");
        var glove = await space.ThingAsync("Glove");
        var (items, _) = Tools(space, space.Vera);

        var found = await items.WhereIs("glove");
        var refused = await TestData.Caught<McpException>(() => items.MoveItems([glove.Id], f.Id));
        var nowhere = await TestData.Caught<McpException>(() => items.MoveItems([glove.Id]));

        await Assert.That(found.Matches.Count).IsEqualTo(1);
        await Assert.That(refused!.Message.Length).IsGreaterThan(0);
        await Assert.That(nowhere!.Message).Contains("toTop");
        await Assert.That(space.Core.Items.Named("Glove").ParentId).IsNull();
    }

    [Test]
    public async Task With_several_spaces_the_assistant_has_to_say_which()
    {
        var space = await ItemSpace.MakeAsync();

        // Spaces are listed oldest first; made in the same millisecond, their ids have no order.
        space.Core.Clock.Now += TimeSpan.FromMinutes(1);
        await space.Core.SpaceManager().CreateSpaceAsync(space.Owen, new("Workshop", null), default);
        await space.ThingAsync("Glove");
        var (items, types) = Tools(space, space.Owen);

        var unnamed = await TestData.Caught<McpException>(() => items.WhereIs("glove"));
        var unknown = await TestData.Caught<McpException>(() => items.WhereIs("glove", space: "Cabin"));
        var named = await items.WhereIs("glove", space: "home");
        var byId = await types.ListTypes(space.SpaceId.ToString());

        await Assert.That(unnamed!.Message).Contains("Home, Workshop");
        await Assert.That(unknown!.Message).Contains("no space called Cabin");
        await Assert.That(named.Matches.Single().Name).IsEqualTo("Glove");
        await Assert.That(byId.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Defining_a_type_and_filling_its_fields_by_search_name()
    {
        var space = await ItemSpace.MakeAsync();
        var (items, types) = Tools(space, space.Owen);
        var made = (await items.AddItems("Millennium Falcon")).Single();

        var lego = await types.DefineType("Lego set", fields:
        [
            new NewField("Set number", Managers.Contracts.Messages.FieldKind.Text, Sortable: true),
            new NewField("Pieces", Managers.Contracts.Messages.FieldKind.Number),
        ]);
        var filled = await items.UpdateItem(made.Id, typeId: lego.Id, fields: new Dictionary<string, string> { ["set_number"] = "75192", ["pieces"] = "7,541" });

        await Assert.That(string.Join(", ", lego.Fields.Select(field => field.Key))).IsEqualTo("set_number, pieces");
        await Assert.That(filled.TypeId).IsEqualTo(lego.Id);
        await Assert.That(filled.Fields[lego.Fields[1].Id!.Value].GetDecimal()).IsEqualTo(7541m);
    }

    [Test]
    public async Task An_assistant_lists_the_saved_views_and_runs_one()
    {
        var space = await ItemSpace.MakeAsync();
        await space.BoxAsync("Shed");
        await space.ThingAsync("Glove");
        await space.Core.ViewManager().CreateViewAsync(space.Owen, space.SpaceId,
            new("Boxes", "is:container", Managers.Contracts.Messages.ViewDisplay.Table, null, null, null, true, true), default);
        var views = new ViewTools(space.Core.ViewManager(), Caller(space, space.Vera));

        var listed = await views.ListViews();
        var run = await views.RunView(listed.Single().Id);
        var missing = await TestData.Caught<McpException>(() => views.RunView(Guid.NewGuid()));

        await Assert.That(listed.Single().Name).IsEqualTo("Boxes");
        await Assert.That(string.Join(", ", run.Results.Select(result => result.Item.Name))).IsEqualTo("Shed");
        await Assert.That(missing!.Message).Contains("view doesn't exist");
    }

    [Test]
    public async Task An_assistant_lends_tracks_stock_and_moves_boxes()
    {
        var space = await ItemSpace.MakeAsync();
        var caller = Caller(space, space.Owen);
        var features = new FeatureTools(space.Core.ItemManager(), caller);
        var projects = new ProjectTools(space.Core.ProjectManager(), caller);
        var drill = await space.ThingAsync("Drill");
        var screws = await space.ThingAsync("Screws", quantity: 10);
        var kitchen = await space.BoxAsync("Kitchen");
        var box = await space.BoxAsync("Box 4");

        await features.LendItem(drill.Id, "Sam", TestData.Sunday.AddDays(5));
        await features.SetLowStock(screws.Id, 4);
        var shelf = await projects.StartProject("Bookshelf");
        await projects.ReserveForProject(shelf.Id, screws.Id, 7);
        await features.SetDestination(box.Id, kitchen.Id);
        var arrived = await features.MarkArrived(box.Id);

        await Assert.That(string.Join(", ", (await features.ListLoans()).Items.Select(item => $"{item.Name} {item.LentTo}"))).IsEqualTo("Drill Sam");
        await Assert.That(string.Join(", ", (await features.ListLowStock()).Items.Select(item => item.Name))).IsEqualTo("Screws");
        await Assert.That((await projects.ListProjects()).Single().Items.Single().Amount).IsEqualTo(7);
        await Assert.That(arrived.ParentId).IsEqualTo(kitchen.Id);
        await Assert.That((await TestData.Caught<McpException>(() => features.LendItem(drill.Id, "Kim")))!.Message).Contains("already lent to Sam");
    }

    /// <summary>The tools as the token of <paramref name="userId"/> sees them.</summary>
    private static (ItemTools Items, TypeTools Types) Tools(ItemSpace space, Guid userId)
    {
        var caller = Caller(space, userId);
        return (new ItemTools(space.Core.ItemManager(), caller), new TypeTools(space.Core.TypeManager(), caller));
    }

    private static McpCaller Caller(ItemSpace space, Guid userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Token"));
        return new McpCaller(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } }, space.Core.SpaceManager());
    }
}
