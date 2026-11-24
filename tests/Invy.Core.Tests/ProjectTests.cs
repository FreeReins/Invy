using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>Projects: setting things aside, using them up, finishing, and how reservations meet low stock.</summary>
public class ProjectTests
{
    private static async Task<(ItemSpace Space, ProjectResponse Shelf, ItemResponse Screws, ItemResponse Saw)> ShelfAsync()
    {
        var space = await ItemSpace.MakeAsync();
        var screws = await space.Items.CreateItemAsync(space.Owen, space.SpaceId,
            ItemSpace.Request("Screws") with { Quantity = 20, LowStockAt = 5 }, default);
        var saw = await space.ThingAsync("Saw");
        var shelf = await space.Core.ProjectManager().CreateProjectAsync(space.Owen, space.SpaceId, new ProjectRequest(" Bookshelf ", null, 0), default);
        return (space, shelf, screws, saw);
    }

    [Test]
    public async Task Setting_aside_keeps_the_amount_but_counts_as_reserved()
    {
        var (space, shelf, screws, saw) = await ShelfAsync();
        var projects = space.Core.ProjectManager();

        await projects.ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(screws.Id, 8), default);
        var after = await projects.ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(saw.Id, 1), default);

        await Assert.That(after.Name).IsEqualTo("Bookshelf");
        await Assert.That(string.Join(", ", after.Items.Select(entry => $"{entry.Item.Name} {entry.Amount}"))).IsEqualTo("Saw 1, Screws 8");
        await Assert.That(space.Core.Items.Named("Screws").Quantity).IsEqualTo(20);
        await Assert.That(space.Core.Items.Named("Screws").Reserved).IsEqualTo(8);
    }

    [Test]
    public async Task Nothing_can_be_set_aside_twice_over()
    {
        var (space, shelf, screws, _) = await ShelfAsync();
        var projects = space.Core.ProjectManager();
        var table = await projects.CreateProjectAsync(space.Owen, space.SpaceId, new ProjectRequest("Table", null, 0), default);
        await projects.ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(screws.Id, 15), default);

        var refused = await TestData.Caught<RuleViolationException>(
            () => projects.ReserveAsync(space.Owen, space.SpaceId, table.Id, new ReserveRequest(screws.Id, 6), default));

        await Assert.That(refused!.Message).IsEqualTo("Only 5 of Screws isn't set aside already.");
    }

    [Test]
    public async Task Setting_aside_can_make_it_low()
    {
        var (space, shelf, screws, _) = await ShelfAsync();

        await space.Core.ProjectManager().ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(screws.Id, 16), default);
        var low = await space.Items.SearchAsync(space.Owen, space.SpaceId, "is:low", null, null, default);

        await Assert.That(space.Core.Messages.Published.OfType<StockRanLow>().Count()).IsEqualTo(1);
        await Assert.That(string.Join(", ", low.Results.Select(hit => hit.Item.Name))).IsEqualTo("Screws");
    }

    [Test]
    public async Task Using_up_takes_it_off_the_amount_with_one_event()
    {
        var (space, shelf, screws, saw) = await ShelfAsync();
        var projects = space.Core.ProjectManager();
        await projects.ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(screws.Id, 8), default);
        await projects.ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(saw.Id, 1), default);

        await projects.UseUpAsync(space.Owen, space.SpaceId, shelf.Id, new UseUpRequest(screws.Id, 3), default);
        var after = await projects.UseUpAsync(space.Owen, space.SpaceId, shelf.Id, new UseUpRequest(saw.Id, null), default);

        var stored = space.Core.Items.Named("Screws");
        await Assert.That(stored.Quantity).IsEqualTo(17);
        await Assert.That(stored.Reserved).IsEqualTo(5);
        await Assert.That(space.Core.Items.Events.Single(e => e.Kind == ItemEventKind.QuantityChanged).QtyDelta).IsEqualTo(-3);
        await Assert.That(space.Core.Items.Named("Saw").Status).IsEqualTo(ItemStatus.Disposed);
        await Assert.That(string.Join(", ", after.Items.Select(entry => $"{entry.Item.Name} {entry.Amount}"))).IsEqualTo("Screws 5");
    }

    [Test]
    public async Task Finishing_releases_what_is_left()
    {
        var (space, shelf, screws, _) = await ShelfAsync();
        var projects = space.Core.ProjectManager();
        await projects.ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(screws.Id, 8), default);

        var done = await projects.FinishProjectAsync(space.Owen, space.SpaceId, shelf.Id, default);
        var open = await projects.ListProjectsAsync(space.Vera, space.SpaceId, false, default);

        await Assert.That(done.Status).IsEqualTo(ProjectStatus.Done);
        await Assert.That(space.Core.Items.Named("Screws").Reserved).IsEqualTo(0);
        await Assert.That(open).IsEmpty();
    }

    [Test]
    public async Task Project_search_finds_what_it_sets_aside()
    {
        var (space, shelf, screws, _) = await ShelfAsync();
        await space.Core.ProjectManager().ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(screws.Id, 2), default);

        var found = await space.Items.SearchAsync(space.Owen, space.SpaceId, "project:bookshelf", null, null, default);

        await Assert.That(string.Join(", ", found.Results.Select(hit => hit.Item.Name))).IsEqualTo("Screws");
    }

    [Test]
    public async Task A_unique_item_is_set_aside_whole()
    {
        var (space, shelf, _, saw) = await ShelfAsync();

        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Core.ProjectManager().ReserveAsync(space.Owen, space.SpaceId, shelf.Id, new ReserveRequest(saw.Id, 2), default))).IsNotNull();
    }

    [Test]
    public async Task Viewers_cant_start_projects()
    {
        var space = await ItemSpace.MakeAsync();

        await Assert.That(await TestData.Caught<NotAllowedException>(
            () => space.Core.ProjectManager().CreateProjectAsync(space.Vera, space.SpaceId, new ProjectRequest("Shelf", null, 0), default))).IsNotNull();
    }
}
