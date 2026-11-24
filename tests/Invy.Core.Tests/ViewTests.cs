using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

public class ViewTests
{
    private static ViewRequest Request(string name, string query = "", bool shared = false, bool pinned = false) =>
        new(name, query, ViewDisplay.Table, null, "name", null, shared, pinned);

    private static async Task<string> NamesAsync(ItemSpace space, Guid viewId, Guid? userId = null) =>
        string.Join(", ", (await space.Core.ViewManager().RunViewAsync(userId ?? space.Owen, space.SpaceId, viewId, null, default))
            .Results.Select(result => result.Item.Name));

    [Test]
    public async Task A_not_seen_in_a_year_view_counts_back_from_when_it_runs()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with { TypeId = space.Tool.Id }, default);
        await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, drill.Id, default);
        var view = await space.Core.ViewManager().CreateViewAsync(space.Owen, space.SpaceId, Request("Not seen in a year", "type:tool seen>1y"), default);

        await Assert.That(await NamesAsync(space, view.Id)).IsEqualTo(string.Empty);

        space.Core.Clock.Now = space.Core.Clock.Now.AddDays(400);

        await Assert.That(await NamesAsync(space, view.Id)).IsEqualTo("Drill");
    }

    [Test]
    public async Task Personal_views_are_their_makers_and_shared_views_are_everyones()
    {
        var space = await ItemSpace.MakeAsync();
        var views = space.Core.ViewManager();
        var mine = await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Owen's tools", "type:tool"), default);
        await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Boxes", "is:container", shared: true), default);
        await views.CreateViewAsync(space.Vera, space.SpaceId, Request("Vera's", "drill"), default);

        var seenByVera = await views.ListViewsAsync(space.Vera, space.SpaceId, default);

        await Assert.That(string.Join(", ", seenByVera.Select(view => $"{view.Name}:{view.CanChange}"))).IsEqualTo("Boxes:False, Vera's:True");
        await Assert.ThrowsAsync<NotFoundException>(() => views.RunViewAsync(space.Vera, space.SpaceId, mine.Id, null, default));
        await Assert.ThrowsAsync<NotFoundException>(() => views.RemoveViewAsync(space.Vera, space.SpaceId, mine.Id, default));
    }

    [Test]
    public async Task Viewers_can_neither_share_views_nor_change_shared_ones()
    {
        var space = await ItemSpace.MakeAsync();
        var views = space.Core.ViewManager();
        var shared = await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Boxes", "is:container", shared: true), default);

        await Assert.ThrowsAsync<NotAllowedException>(() => views.CreateViewAsync(space.Vera, space.SpaceId, Request("Mine", shared: true), default));
        await Assert.ThrowsAsync<NotAllowedException>(() => views.UpdateViewAsync(space.Vera, space.SpaceId, shared.Id, Request("Renamed"), default));
        await Assert.ThrowsAsync<NotAllowedException>(() => views.RemoveViewAsync(space.Vera, space.SpaceId, shared.Id, default));
    }

    [Test]
    public async Task Pinned_views_come_first_then_by_name()
    {
        var space = await ItemSpace.MakeAsync();
        var views = space.Core.ViewManager();
        await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Bravo"), default);
        await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Charlie", pinned: true), default);
        var alpha = await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Alpha"), default);
        await views.UpdateViewAsync(space.Owen, space.SpaceId, alpha.Id, Request("Delta", pinned: true), default);

        var listed = await views.ListViewsAsync(space.Owen, space.SpaceId, default);

        await Assert.That(string.Join(", ", listed.Select(view => view.Name))).IsEqualTo("Charlie, Delta, Bravo");
    }

    [Test]
    public async Task A_view_is_checked_before_it_is_saved()
    {
        var space = await ItemSpace.MakeAsync();
        var views = space.Core.ViewManager();

        await Assert.ThrowsAsync<RuleViolationException>(() => views.CreateViewAsync(space.Owen, space.SpaceId, Request(" "), default));
        await Assert.ThrowsAsync<RuleViolationException>(() => views.CreateViewAsync(space.Owen, space.SpaceId, Request("Bad", "colour:red"), default));
        await Assert.ThrowsAsync<RuleViolationException>(() => views.CreateViewAsync(space.Owen, space.SpaceId,
            Request("Bad") with { Columns = ["name", "colour"] }, default));
        await Assert.ThrowsAsync<RuleViolationException>(() => views.CreateViewAsync(space.Owen, space.SpaceId,
            Request("Board") with { Display = ViewDisplay.Board, GroupBy = "brand" }, default));
        await Assert.That(space.Core.Views.Rows.Count).IsEqualTo(0);
    }

    [Test]
    public async Task A_board_groups_by_a_select_field_and_a_table_keeps_its_columns()
    {
        var space = await ItemSpace.MakeAsync();
        var views = space.Core.ViewManager();

        var board = await views.CreateViewAsync(space.Owen, space.SpaceId,
            Request("By power", "type:tool") with { Display = ViewDisplay.Board, GroupBy = "Power" }, default);
        var table = await views.CreateViewAsync(space.Owen, space.SpaceId,
            Request("Tools", "type:tool") with { Columns = ["Name", "brand", "price", "name"] }, default);
        var plain = await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Everything"), default);

        await Assert.That(board.GroupBy).IsEqualTo("power");
        await Assert.That(string.Join(", ", table.Columns)).IsEqualTo("name, brand, price");
        await Assert.That(string.Join(", ", plain.Columns)).IsEqualTo("name, type, place");
    }

    [Test]
    public async Task Removing_a_view_leaves_its_items()
    {
        var space = await ItemSpace.MakeAsync();
        await space.BoxAsync("Shed");
        var views = space.Core.ViewManager();
        var view = await views.CreateViewAsync(space.Owen, space.SpaceId, Request("Boxes", "is:container"), default);

        await views.RemoveViewAsync(space.Owen, space.SpaceId, view.Id, default);

        await Assert.That(space.Core.Views.Rows.Count).IsEqualTo(0);
        await Assert.That((await space.Items.SearchAsync(space.Owen, space.SpaceId, "is:container", null, null, default)).Total).IsEqualTo(1);
    }

    [Test]
    public async Task A_views_results_carry_the_details_its_columns_and_cards_show()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with { TypeId = space.Tool.Id }, default);
        await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, drill.Id, default);
        var view = await space.Core.ViewManager().CreateViewAsync(space.Owen, space.SpaceId, Request("Tools", "type:tool"), default);

        var found = (await space.Core.ViewManager().RunViewAsync(space.Owen, space.SpaceId, view.Id, null, default)).Results.Single();
        var searched = (await space.Items.SearchAsync(space.Owen, space.SpaceId, "type:tool", null, null, default)).Results.Single();

        await Assert.That(found.Details?.LastSeenAt).IsEqualTo(space.Core.Clock.Now);
        await Assert.That(found.Details?.PhotoId).IsNull();
        await Assert.That(searched.Details).IsNull();
    }

    [Test]
    public async Task A_timeline_view_shows_the_events_of_the_items_it_finds()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with { TypeId = space.Tool.Id }, default);
        await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Teapot"), default);
        space.Core.Clock.Now = space.Core.Clock.Now.AddMinutes(1);
        await space.Items.MarkSeenAsync(space.Owen, space.SpaceId, drill.Id, default);
        var view = await space.Core.ViewManager().CreateViewAsync(space.Owen, space.SpaceId, Request("Tools", "type:tool"), default);

        var timeline = await space.Core.ViewManager().ViewTimelineAsync(space.Owen, space.SpaceId, view.Id, null, default);

        await Assert.That(string.Join(", ", timeline.Events.Select(e => $"{e.Item.Name} {e.Kind}"))).IsEqualTo("Drill Seen, Drill Created");
        await Assert.That(timeline.Next).IsNull();
    }
}
