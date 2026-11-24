using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Tests;

/// <summary>Lending and returning, overdue loans in search and the daily reminders.</summary>
public class LendingTests
{
    private static async Task<(ItemSpace Space, ItemResponse Drill)> DrillAsync(bool lendingOn = true)
    {
        var space = await ItemSpace.MakeAsync();
        space.Core.Spaces.Rows.Single().Features = lendingOn ? [SpaceFeatures.Lending] : [];
        space.Core.Settings.Notifications.WebhookUrl = "https://hooks.example/invy";
        return (space, await space.ThingAsync("Drill"));
    }

    private static DateOnly Today => TestData.Sunday;

    [Test]
    public async Task Lending_sets_who_and_when_with_one_event()
    {
        var (space, drill) = await DrillAsync();

        var lent = await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("  Sam ", Today.AddDays(7)), default);

        await Assert.That(lent.Status).IsEqualTo(ItemStatus.Lent);
        await Assert.That(lent.LentTo).IsEqualTo("Sam");
        await Assert.That(lent.DueOn).IsEqualTo(Today.AddDays(7));
        var lentEvent = space.Core.Items.Events.Single(e => e.Kind == ItemEventKind.Lent);
        await Assert.That(string.Join(", ", lentEvent.Changes.Select(change => $"{change.What}={change.After}"))).IsEqualTo("Lent to=Sam, Due back=2026-11-15");
    }

    [Test]
    public async Task Returning_makes_it_active_again()
    {
        var (space, drill) = await DrillAsync();
        await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", null), default);

        var back = await space.Items.ReturnAsync(space.Owen, space.SpaceId, drill.Id, default);

        await Assert.That(back.Status).IsEqualTo(ItemStatus.Active);
        await Assert.That(back.LentTo).IsNull();
        await Assert.That(space.Core.Items.Events.Count(e => e.Kind == ItemEventKind.Returned)).IsEqualTo(1);
    }

    [Test]
    public async Task Something_lent_cant_be_lent_again()
    {
        var (space, drill) = await DrillAsync();
        await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", null), default);

        var refused = await TestData.Caught<RuleViolationException>(() => space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Kim", null), default));

        await Assert.That(refused!.Message).Contains("already lent to Sam");
    }

    [Test]
    public async Task A_due_date_in_the_past_is_refused()
    {
        var (space, drill) = await DrillAsync();

        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", Today.AddDays(-1)), default))).IsNotNull();
    }

    [Test]
    public async Task Overdue_loans_are_found_and_listed()
    {
        var (space, drill) = await DrillAsync();
        var saw = await space.ThingAsync("Saw");
        await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", Today), default);
        await space.Items.LendAsync(space.Owen, space.SpaceId, saw.Id, new LendRequest("Kim", Today.AddDays(5)), default);
        space.Core.Clock.Now = space.Core.Clock.Now.AddDays(2);

        var overdue = await space.Items.SearchAsync(space.Owen, space.SpaceId, "is:overdue", null, null, default);
        var toKim = await space.Items.SearchAsync(space.Owen, space.SpaceId, "lent:ki", null, null, default);
        var loans = await space.Items.ListLoansAsync(space.Vera, space.SpaceId, false, default);

        await Assert.That(string.Join(", ", overdue.Results.Select(hit => hit.Item.Name))).IsEqualTo("Drill");
        await Assert.That(string.Join(", ", toKim.Results.Select(hit => hit.Item.Name))).IsEqualTo("Saw");
        await Assert.That(string.Join(", ", loans.Items.Select(item => item.Name))).IsEqualTo("Drill, Saw");
    }

    [Test]
    public async Task The_daily_reminders_list_loans_due_back()
    {
        var (space, drill) = await DrillAsync();
        await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", Today.AddDays(1)), default);
        space.Core.Clock.Now = space.Core.Clock.Now.AddDays(3);
        var maintenance = space.Core.MaintenanceManager();

        await maintenance.SendTodayAsync(default);
        await maintenance.SendReminderAsync(space.Core.Messages.Published.OfType<SendReminder>().Single(), default);

        var sent = space.Core.Webhook.Sent.Single();
        await Assert.That(sent.Type).IsEqualTo(NotificationTypes.Overdue);
        await Assert.That(sent.Message).IsEqualTo("1 thing is due back: Drill from Sam (since 9 Nov 2026).");
    }

    [Test]
    public async Task No_loan_reminders_when_the_space_has_lending_off()
    {
        var (space, drill) = await DrillAsync(lendingOn: false);
        await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", Today), default);
        space.Core.Clock.Now = space.Core.Clock.Now.AddDays(3);

        await space.Core.MaintenanceManager().SendTodayAsync(default);

        await Assert.That(space.Core.Messages.Published.OfType<SendReminder>()).IsEmpty();
    }
}
