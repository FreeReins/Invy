using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Tests;

/// <summary>Low-stock levels, the alert when something runs low, and the daily list (docs/developers/design.md, Audience features).</summary>
public class StockTests
{
    private static async Task<(ItemSpace Space, ItemResponse Screws)> ScrewsAsync(bool stockOn = true)
    {
        var space = await ItemSpace.MakeAsync();
        space.Core.Spaces.Rows.Single().Features = stockOn ? [SpaceFeatures.Stock] : [];
        space.Core.Settings.Notifications.WebhookUrl = "https://hooks.example/invy";
        var screws = await space.Items.CreateItemAsync(space.Owen, space.SpaceId,
            ItemSpace.Request("Screws") with { Quantity = 20, Unit = "boxes", LowStockAt = 5 }, default);
        return (space, screws);
    }

    [Test]
    public async Task Only_counted_items_can_have_a_level()
    {
        var space = await ItemSpace.MakeAsync();

        await Assert.That(await TestData.Caught<RuleViolationException>(() => space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with { LowStockAt = 1 }, default))).IsNotNull();
    }

    [Test]
    public async Task Taking_it_to_its_level_queues_one_alert()
    {
        var (space, screws) = await ScrewsAsync();

        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-10), default);
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-5), default);
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-1), default);

        await Assert.That(space.Core.Messages.Published.OfType<StockRanLow>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Topping_up_then_running_low_again_alerts_again()
    {
        var (space, screws) = await ScrewsAsync();

        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-16), default);
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(10), default);
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-10), default);

        await Assert.That(space.Core.Messages.Published.OfType<StockRanLow>().Count()).IsEqualTo(2);
    }

    [Test]
    public async Task Raising_the_level_above_the_amount_alerts()
    {
        var (space, screws) = await ScrewsAsync();

        var changed = await space.Items.SetLowStockAsync(space.Owen, space.SpaceId, screws.Id, new LowStockRequest(25), default);

        await Assert.That(changed.IsLow).IsTrue();
        await Assert.That(space.Core.Messages.Published.OfType<StockRanLow>().Count()).IsEqualTo(1);
        await Assert.That(space.Core.Items.Events.Last().Changes.Single().What).IsEqualTo("Low stock at");
    }

    [Test]
    public async Task The_alert_reaches_the_webhook_with_the_amount_left()
    {
        var (space, screws) = await ScrewsAsync();
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-17), default);

        await space.Core.MaintenanceManager().SendLowStockAsync(new StockRanLow(space.SpaceId, screws.Id), default);

        var sent = space.Core.Webhook.Sent.Single();
        await Assert.That(sent.Type).IsEqualTo(NotificationTypes.LowStock);
        await Assert.That(sent.Message).Contains("down to 3 boxes");
    }

    [Test]
    public async Task No_alert_when_the_space_has_stock_off()
    {
        var (space, screws) = await ScrewsAsync(stockOn: false);
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-17), default);

        await space.Core.MaintenanceManager().SendLowStockAsync(new StockRanLow(space.SpaceId, screws.Id), default);
        await space.Core.MaintenanceManager().SendTodayAsync(default);

        await Assert.That(space.Core.Webhook.Sent).IsEmpty();
        await Assert.That(space.Core.Messages.Published.OfType<SendReminder>()).IsEmpty();
    }

    [Test]
    public async Task No_alert_when_it_was_topped_up_before_the_alert_went_out()
    {
        var (space, screws) = await ScrewsAsync();
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-17), default);
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(17), default);

        await space.Core.MaintenanceManager().SendLowStockAsync(new StockRanLow(space.SpaceId, screws.Id), default);

        await Assert.That(space.Core.Webhook.Sent).IsEmpty();
    }

    [Test]
    public async Task The_daily_reminders_list_what_is_still_low()
    {
        var (space, screws) = await ScrewsAsync();
        await space.Items.AdjustQuantityAsync(space.Owen, space.SpaceId, screws.Id, new AdjustQuantityRequest(-15), default);
        var maintenance = space.Core.MaintenanceManager();

        var run = await maintenance.SendTodayAsync(default);
        var queued = space.Core.Messages.Published.OfType<SendReminder>().Single();
        await maintenance.SendReminderAsync(queued, default);

        await Assert.That(string.Join(", ", run.Sent)).IsEqualTo(NotificationTypes.LowStockDaily);
        await Assert.That(space.Core.Webhook.Sent.Single().Message).IsEqualTo("1 thing is running low: Screws (5 boxes left).");
    }

    [Test]
    public async Task Is_low_finds_what_is_at_or_below_its_level()
    {
        var (space, screws) = await ScrewsAsync();
        await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Nails") with { Quantity = 2, LowStockAt = 3 }, default);
        await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Glue") with { Quantity = 2 }, default);

        var found = await space.Items.SearchAsync(space.Owen, space.SpaceId, "is:low", null, null, default);
        var listed = await space.Items.ListLowStockAsync(space.Vera, space.SpaceId, default);

        await Assert.That(string.Join(", ", found.Results.Select(hit => hit.Item.Name))).IsEqualTo("Nails");
        await Assert.That(string.Join(", ", listed.Items.Select(item => item.Name))).IsEqualTo("Nails");
    }

    [Test]
    public async Task Viewers_cant_set_a_level()
    {
        var (space, screws) = await ScrewsAsync();

        await Assert.That(await TestData.Caught<NotAllowedException>(() => space.Items.SetLowStockAsync(space.Vera, space.SpaceId, screws.Id, new LowStockRequest(2), default))).IsNotNull();
    }
}
