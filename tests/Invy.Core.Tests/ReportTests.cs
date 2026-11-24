using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

public class ReportTests
{
    /// <summary>A kitchen holding a priced drill in a drawer, a garage holding a baseball, and a hammer at the top.</summary>
    private static async Task<ItemSpace> HomeAsync()
    {
        var space = await ItemSpace.MakeAsync();
        var kitchen = await space.BoxAsync("Kitchen");
        var drawer = await space.BoxAsync("Drawer", kitchen);
        await ToolAsync(space, "Drill", 129.5m, drawer);
        var garage = await space.BoxAsync("Garage");
        await space.ThingAsync("Baseball", garage);
        await ToolAsync(space, "Hammer", 20m, null);
        foreach (var message in space.Core.Messages.Published.OfType<ItemsChanged>().ToList())
        {
            await space.Items.UpdateRollupsAsync(message, default);
        }

        return space;
    }

    private static Task<ItemResponse> ToolAsync(ItemSpace space, string name, decimal price, ItemResponse? parent) =>
        space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request(name) with
        {
            TypeId = space.Tool.Id,
            ParentId = parent?.Id,
            Fields = new Dictionary<Guid, System.Text.Json.JsonElement>
            {
                [space.Field("price")] = ItemSpace.Json(price),
                [space.Field("brand")] = ItemSpace.Json("=HYPERLINK(\"x\")"),
            },
        }, default);

    private static async Task<List<string>> ReadAsync(IAsyncEnumerable<string> lines)
    {
        var read = new List<string>();
        await foreach (var line in lines)
        {
            read.Add(line);
        }

        return read;
    }

    [Test]
    public async Task Value_totals_come_by_place_at_the_top_and_by_type()
    {
        var space = await HomeAsync();

        var totals = await space.Core.ReportManager().ValueTotalsAsync(space.Vera, space.SpaceId, default);

        await Assert.That(totals.Total).IsEqualTo(149.5m);
        await Assert.That(string.Join(", ", totals.ByPlace.Select(place => $"{place.Place?.Name ?? "top"} {place.Count} {place.Value}")))
            .IsEqualTo("Kitchen 2 129.5, top 1 20, Garage 1 0");
        await Assert.That(string.Join(", ", totals.ByType.Select(type => $"{type.Name} {type.Count}/{type.Valued} {type.Value}")))
            .IsEqualTo("Tool 2/2 149.5, Box 3/0 0, No type 1/0 0");
        await Assert.That(totals.Unvalued).IsEqualTo(4);
    }

    [Test]
    public async Task The_insurance_inventory_lists_things_by_room_without_the_containers()
    {
        var space = await HomeAsync();

        var report = await space.Core.ReportManager().InsuranceAsync(space.Owen, space.SpaceId, default);

        await Assert.That(string.Join("; ", report.Rooms.Select(room =>
                $"{room.Room?.Name ?? "top"} {room.Value}: {string.Join(", ", room.Items.Select(result => result.Item.Name))}")))
            .IsEqualTo("Garage 0: Baseball; Kitchen 129.5: Drill; top 20: Hammer");
        await Assert.That(report.Total).IsEqualTo(149.5m);
        await Assert.That(report.Truncated).IsFalse();
    }

    [Test]
    public async Task The_insurance_csv_has_a_row_per_thing_and_keeps_formulas_as_text()
    {
        var space = await HomeAsync();

        var file = await space.Core.ReportManager().InsuranceCsvAsync(space.Owen, space.SpaceId, default);
        var lines = await ReadAsync(file.Lines);

        await Assert.That(file.Name).IsEqualTo("Home insurance inventory 2026-11-08.csv");
        await Assert.That(lines[0]).StartsWith("Room,Place,Name,Marking,Type,Amount,Unit,Value,Status,");
        await Assert.That(lines.Count).IsEqualTo(4);
        await Assert.That(lines[2]).StartsWith("Kitchen,Kitchen › Drawer,Drill,,Tool,,,129.5,Active,");
        await Assert.That(lines[2]).Contains(",\"'=HYPERLINK(\"\"x\"\")\",");
    }

    [Test]
    public async Task The_missing_report_lists_what_an_audit_did_not_find()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box F");
        await space.ThingAsync("Baseball", box);
        var marble = await space.ThingAsync("Marble", box);
        await space.Items.AuditAsync(space.Owen, space.SpaceId, box.Id, new AuditRequest([marble.Id]), default);

        var missing = await space.Core.ReportManager().MissingAsync(space.Owen, space.SpaceId, null, default);
        var csv = await ReadAsync((await space.Core.ReportManager().MissingCsvAsync(space.Owen, space.SpaceId, default)).Lines);

        await Assert.That(string.Join(", ", missing.Results.Select(result => result.Item.Name))).IsEqualTo("Baseball");
        await Assert.That(missing.Results[0].Chain[0].Name).IsEqualTo("Box F");
        await Assert.That(csv.Count).IsEqualTo(2);
    }

    [Test]
    public async Task A_view_exports_everything_it_finds()
    {
        var space = await HomeAsync();
        var view = await space.Core.ViewManager().CreateViewAsync(space.Owen, space.SpaceId,
            new ViewRequest("Tools", "type:tool", ViewDisplay.Cards, null, "name", null, false, false), default);

        var file = await space.Core.ReportManager().ViewCsvAsync(space.Owen, space.SpaceId, view.Id, default);
        var lines = await ReadAsync(file.Lines);

        await Assert.That(file.Name).IsEqualTo("Home Tools 2026-11-08.csv");
        await Assert.That(string.Join(", ", lines.Skip(1).Select(line => line.Split(',')[2]))).IsEqualTo("Drill, Hammer");
    }

    [Test]
    public async Task Csv_cells_are_quoted_only_when_they_need_it()
    {
        var engine = new ReportEngine();

        await Assert.That(engine.Line(["plain", "a,b", "say \"hi\"", null, "-5", "-x", "@me"]))
            .IsEqualTo("plain,\"a,b\",\"say \"\"hi\"\"\",,-5,'-x,'@me\r\n");
    }
}
