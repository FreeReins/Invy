using System.Text.Json;
using Invy.Core.Engines;
using Invy.Core.Engines.Contracts.Messages;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Tests;

/// <summary>Date fields that remind: warranties, upkeep that repeats, and the daily reminders.</summary>
public class RemindersTests
{
    private static DateOnly Today => TestData.Sunday;

    private static async Task<(ItemSpace Space, ItemTypeResponse Appliance)> SpaceAsync(bool remindersOn = true)
    {
        var space = await ItemSpace.MakeAsync();
        space.Core.Spaces.Rows.Single().Features = remindersOn ? [SpaceFeatures.Reminders] : [];
        space.Core.Settings.Notifications.WebhookUrl = "https://hooks.example/invy";
        var appliance = await space.Core.TypeManager().CreateTypeAsync(space.Owen, space.SpaceId, new ItemTypeRequest("Appliance", null, null, false,
        [
            new FieldDefinition(null, "warranty", "Warranty until", FieldKind.Date, null, [], false, false, Remind: 30),
            new FieldDefinition(null, "service", "Next service", FieldKind.Date, null, [], false, false, Remind: 7, RepeatMonths: 6),
        ], 0), default);
        return (space, appliance);
    }

    private static Guid Field(ItemTypeResponse type, string key) => type.Fields.Single(field => field.Key == key).Id!.Value;

    private static Task<ItemResponse> ApplianceAsync(ItemSpace space, ItemTypeResponse type, string name, DateOnly? warranty = null, DateOnly? service = null)
    {
        var fields = new Dictionary<Guid, JsonElement>();
        if (warranty is DateOnly w)
        {
            fields[Field(type, "warranty")] = ItemSpace.Json(w.ToString("yyyy-MM-dd"));
        }

        if (service is DateOnly s)
        {
            fields[Field(type, "service")] = ItemSpace.Json(s.ToString("yyyy-MM-dd"));
        }

        return space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request(name) with { TypeId = type.Id, Fields = fields }, default);
    }

    [Test]
    public async Task A_reminder_date_is_always_sortable()
    {
        var (_, appliance) = await SpaceAsync();

        await Assert.That(appliance.Fields.All(field => field.Sortable)).IsTrue();
    }

    [Test]
    public async Task Only_date_fields_can_remind()
    {
        var space = await ItemSpace.MakeAsync();
        var request = new ItemTypeRequest("Thing", null, null, false,
            [new FieldDefinition(null, "size", "Size", FieldKind.Number, null, [], false, false, Remind: 3)], 0);

        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Core.TypeManager().CreateTypeAsync(space.Owen, space.SpaceId, request, default))).IsNotNull();
    }

    [Test]
    public async Task Reminders_go_on_the_heads_up_day_and_the_day_itself()
    {
        var warranty = new TypeField { Label = "Warranty until", Kind = FieldKind.Date, Remind = 30 };
        var service = new TypeField { Label = "Next service", Kind = FieldKind.Date, Remind = 7, RepeatMonths = 6 };
        var space = new Space { Name = "Home" };
        var item = new Item { Name = "Boiler" };
        bool Remind(TypeField field, int days) => NotificationEngine.Remind(new DueDate(space, item, field, Today.AddDays(days)), Today);

        await Assert.That(Remind(warranty, 30)).IsTrue();
        await Assert.That(Remind(warranty, 29)).IsFalse();
        await Assert.That(Remind(warranty, 0)).IsTrue();
        await Assert.That(Remind(warranty, -7)).IsFalse();
        await Assert.That(Remind(service, -7)).IsTrue();
        await Assert.That(Remind(service, -8)).IsFalse();
    }

    [Test]
    public async Task The_daily_reminders_name_the_date()
    {
        var (space, appliance) = await SpaceAsync();
        await ApplianceAsync(space, appliance, "Fridge", warranty: Today.AddDays(30));
        await ApplianceAsync(space, appliance, "Oven", warranty: Today.AddDays(12));
        var maintenance = space.Core.MaintenanceManager();

        await maintenance.SendTodayAsync(default);
        await maintenance.SendReminderAsync(space.Core.Messages.Published.OfType<SendReminder>().Single(), default);

        await Assert.That(space.Core.Webhook.Sent.Single().Message)
            .IsEqualTo("1 thing is to remember: Fridge: Warranty until is on 8 Dec 2026, in 30 days.");
    }

    [Test]
    public async Task No_date_reminders_when_the_space_has_reminders_off()
    {
        var (space, appliance) = await SpaceAsync(remindersOn: false);
        await ApplianceAsync(space, appliance, "Fridge", warranty: Today);

        await space.Core.MaintenanceManager().SendTodayAsync(default);

        await Assert.That(space.Core.Messages.Published.OfType<SendReminder>()).IsEmpty();
    }

    [Test]
    public async Task The_list_keeps_overdue_upkeep_but_not_a_passed_warranty()
    {
        var (space, appliance) = await SpaceAsync();
        await ApplianceAsync(space, appliance, "Boiler", service: Today.AddDays(-20));
        await ApplianceAsync(space, appliance, "Fridge", warranty: Today.AddDays(-20));
        await ApplianceAsync(space, appliance, "Oven", warranty: Today.AddDays(10));

        var listed = await space.Items.ListRemindersAsync(space.Vera, space.SpaceId, 30, default);

        await Assert.That(string.Join(", ", listed.Dates.Select(date => $"{date.Item.Name} {date.Days}"))).IsEqualTo("Boiler -20, Oven 10");
    }

    [Test]
    public async Task Marking_upkeep_done_moves_the_date_on_from_today()
    {
        var (space, appliance) = await SpaceAsync();
        var boiler = await ApplianceAsync(space, appliance, "Boiler", service: Today.AddDays(-20));

        var done = await space.Items.MarkUpkeepDoneAsync(space.Owen, space.SpaceId, boiler.Id, new UpkeepDoneRequest(Field(appliance, "service")), default);

        await Assert.That(done.Fields[Field(appliance, "service")].GetString()).IsEqualTo("2027-05-08");
        await Assert.That(space.Core.Items.Events.Last().Changes.Single().What).IsEqualTo("Next service");
        var found = await space.Items.SearchAsync(space.Owen, space.SpaceId, "due<30d", null, null, default);
        await Assert.That(found.Results).IsEmpty();
    }

    [Test]
    public async Task A_warranty_cant_be_marked_done()
    {
        var (space, appliance) = await SpaceAsync();
        var fridge = await ApplianceAsync(space, appliance, "Fridge", warranty: Today);

        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.MarkUpkeepDoneAsync(space.Owen, space.SpaceId, fridge.Id, new UpkeepDoneRequest(Field(appliance, "warranty")), default))).IsNotNull();
    }

    [Test]
    public async Task Due_finds_reminder_dates_and_loans()
    {
        var (space, appliance) = await SpaceAsync();
        await ApplianceAsync(space, appliance, "Fridge", warranty: Today.AddDays(20));
        await ApplianceAsync(space, appliance, "Oven", warranty: Today.AddDays(90));
        var drill = await space.ThingAsync("Drill");
        await space.Items.LendAsync(space.Owen, space.SpaceId, drill.Id, new LendRequest("Sam", Today.AddDays(3)), default);

        var found = await space.Items.SearchAsync(space.Owen, space.SpaceId, "due<30d", "name", null, default);

        await Assert.That(string.Join(", ", found.Results.Select(hit => hit.Item.Name))).IsEqualTo("Drill, Fridge");
    }
}
