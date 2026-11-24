using System.Text;
using Invy.Core.Engines;
using Invy.Core.Managers;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>Imports from CSV and Homebox's export: reading the file, mapping columns, and running in batches.</summary>
public class ImportTests
{
    private static readonly ImportEngine Engine = new();

    private const string Homebox =
        "HB.import_ref,HB.location,HB.labels,HB.asset_id,HB.archived,HB.name,HB.quantity,HB.description,HB.manufacturer,HB.serial_number\n" +
        ",Garage / Shelf 2,Tools;Power,000-001,false,Drill,1,\"Cordless, 18V\",Makita,SN123\n" +
        ",Garage / Shelf 2,,000-002,false,Screws,40,,,\n" +
        ",Garage,,,false,,1,,,\n";

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>Runs each queued import run in turn, as the imports queue would, including the ones runs queue.</summary>
    private static async Task RunQueuedAsync(ItemSpace space)
    {
        for (var next = 0; next < space.Core.Messages.Published.Count; next++)
        {
            if (space.Core.Messages.Published[next] is RunImport run)
            {
                await space.Core.ImportManager().RunImportAsync(run, default);
            }
        }
    }

    [Test]
    public async Task Quoted_cells_keep_their_commas_and_quotes()
    {
        var rows = Engine.Parse(Bytes("Name,Notes\r\n\"Box \"\"A\"\"\",\"one, two\"\r\n\r\nLamp,\n"));

        await Assert.That(rows.Count).IsEqualTo(3);
        await Assert.That(rows[1][0]).IsEqualTo("Box \"A\"");
        await Assert.That(rows[1][1]).IsEqualTo("one, two");
    }

    [Test]
    public async Task Semicolon_files_are_read_too()
    {
        var rows = Engine.Parse(Bytes("Name;Qty\nNails;200\n"));

        await Assert.That(rows[1][1]).IsEqualTo("200");
    }

    [Test]
    public async Task Homebox_exports_are_recognised_and_mapped()
    {
        var rows = Engine.Parse(Bytes(Homebox));
        var kind = ImportEngine.KindOf(rows[0]);
        var mapping = Engine.Suggest(rows[0], kind, null);

        await Assert.That(kind).IsEqualTo(ImportKind.Homebox);
        await Assert.That(string.Join(", ", mapping.Select(entry => entry.Target)))
            .IsEqualTo("ignore, location, tags, marking, ignore, name, quantity, notes, note, note");
    }

    [Test]
    public async Task A_plain_csv_is_mapped_by_its_headings()
    {
        var rows = Engine.Parse(Bytes("Item,Qty,Where,Colour\nNails,200,Shed,grey\n"));

        var mapping = Engine.Suggest(rows[0], ImportEngine.KindOf(rows[0]), null);

        await Assert.That(string.Join(", ", mapping.Select(entry => entry.Target))).IsEqualTo("name, quantity, location, note");
    }

    [Test]
    public async Task A_mapping_needs_one_name_column()
    {
        var refused = await TestData.Caught<RuleViolationException>(() =>
            Task.FromResult(ImportEngine.Check([new ColumnMapping(0, "notes")], 2, null)));

        await Assert.That(refused!.Message).IsEqualTo("Pick exactly one column for the name.");
    }

    [Test]
    public async Task A_homebox_import_makes_its_locations_and_items()
    {
        var space = await ItemSpace.MakeAsync();
        var imports = space.Core.ImportManager();
        var house = await space.BoxAsync("House");

        var uploaded = await imports.UploadAsync(space.Owen, space.SpaceId, new UploadImportRequest("export.csv", Bytes(Homebox)), default);
        var started = await imports.StartImportAsync(space.Owen, space.SpaceId, uploaded.Id, new StartImportRequest(uploaded.Mapping, house.Id, null), default);
        await RunQueuedAsync(space);
        var done = await imports.GetImportAsync(space.Vera, space.SpaceId, uploaded.Id, default);

        await Assert.That(uploaded.Sample.Count).IsEqualTo(3);
        await Assert.That(started.Status).IsEqualTo(ImportStatus.Queued);
        await Assert.That(done.Status).IsEqualTo(ImportStatus.Done);
        await Assert.That(done.Added).IsEqualTo(4);
        await Assert.That(string.Join(", ", done.Problems.Select(problem => $"{problem.Row}: {problem.Problem}"))).IsEqualTo("4: It has no name.");
        var drill = space.Core.Items.Named("Drill");
        var shelf = space.Core.Items.Named("Shelf 2");
        await Assert.That(drill.ParentId).IsEqualTo(shelf.Id);
        await Assert.That(space.Core.Items.Named("Garage").ParentId).IsEqualTo(house.Id);
        await Assert.That(drill.Quantity).IsNull();
        await Assert.That(drill.Marking).IsEqualTo("000-001");
        await Assert.That(string.Join(", ", drill.Tags)).IsEqualTo("tools, power");
        await Assert.That(drill.Notes).IsEqualTo("Cordless, 18V\n\nManufacturer: Makita\n\nSerial number: SN123");
        await Assert.That(space.Core.Items.Named("Screws").Quantity).IsEqualTo(40);
        await Assert.That(space.Core.Files.Files).IsEmpty();
    }

    [Test]
    public async Task Columns_can_fill_the_import_types_fields()
    {
        var space = await ItemSpace.MakeAsync();
        var imports = space.Core.ImportManager();
        var uploaded = await imports.UploadAsync(space.Owen, space.SpaceId,
            new UploadImportRequest("tools.csv", Bytes("Name,Brand,Price\nDrill,Makita,129.99\nSaw,Bosch,lots\n")), default);

        await imports.StartImportAsync(space.Owen, space.SpaceId, uploaded.Id, new StartImportRequest(
            [new ColumnMapping(0, "name"), new ColumnMapping(1, $"field:{space.Field("brand")}"), new ColumnMapping(2, $"field:{space.Field("price")}")],
            null, space.Tool.Id), default);
        await RunQueuedAsync(space);

        var drill = space.Core.Items.Named("Drill");
        await Assert.That(drill.TypeId).IsEqualTo(space.Tool.Id);
        await Assert.That(drill.Fields[space.Field("brand")].GetString()).IsEqualTo("Makita");
        await Assert.That(drill.Value).IsEqualTo(129.99m);
        await Assert.That(space.Core.Items.Values.Count(value => value.ItemId == drill.Id)).IsEqualTo(2);
        await Assert.That(space.Core.Items.Rows.Any(item => item.Name == "Saw")).IsFalse();
    }

    [Test]
    public async Task A_big_file_goes_one_batch_per_run_and_running_again_adds_nothing_more()
    {
        var space = await ItemSpace.MakeAsync();
        var imports = space.Core.ImportManager();
        var csv = new StringBuilder("Name\n");
        for (var i = 0; i < ImportManager.BatchSize + 10; i++)
        {
            csv.Append("Part ").Append(i).Append('\n');
        }

        var uploaded = await imports.UploadAsync(space.Owen, space.SpaceId, new UploadImportRequest("parts.csv", Bytes(csv.ToString())), default);
        await imports.StartImportAsync(space.Owen, space.SpaceId, uploaded.Id, new StartImportRequest(uploaded.Mapping, null, null), default);
        var run = space.Core.Messages.Published.OfType<RunImport>().Single();
        await imports.RunImportAsync(run, default);
        var queuedAfterFirst = space.Core.Messages.Published.OfType<RunImport>().Count();
        await RunQueuedAsync(space);
        await imports.RunImportAsync(run, default);

        await Assert.That(queuedAfterFirst).IsEqualTo(2);
        await Assert.That(space.Core.Items.BulkInserts).IsEqualTo(2);
        await Assert.That(space.Core.Imports.Rows.Single().Status).IsEqualTo(ImportStatus.Done);
        await Assert.That(space.Core.Items.Rows.Count).IsEqualTo(ImportManager.BatchSize + 10);
        await Assert.That(space.Core.Items.Events.Count).IsEqualTo(ImportManager.BatchSize + 10);
    }

    [Test]
    public async Task Viewers_cant_import()
    {
        var space = await ItemSpace.MakeAsync();

        await Assert.That(await TestData.Caught<NotAllowedException>(() => space.Core.ImportManager().UploadAsync(
            space.Vera, space.SpaceId, new UploadImportRequest("a.csv", Bytes("Name\nA\n")), default))).IsNotNull();
    }
}
