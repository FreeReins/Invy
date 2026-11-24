using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Persistence.Migrations;
using Invy.Core.Utilities;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Invy.Core.Tests;

/// <summary>Kinds of space: what a space starts with, its feature switches, examples and getting-started card.</summary>
public class SpaceKindTests
{
    /// <summary>A core with Owen, Eddie and Vera as accounts, and no spaces.</summary>
    private static TestCore People()
    {
        var core = new TestCore();
        foreach (var name in new[] { "owen", "eddie", "vera" })
        {
            core.Accounts.Users.Add(new UserAccount { Username = name, DisplayName = char.ToUpperInvariant(name[0]) + name[1..] });
        }

        return core;
    }

    private static Guid Id(TestCore core, string username) => core.Accounts.User(username).Id;

    /// <summary>A home made by Owen with Eddie as an editor and Vera as a viewer.</summary>
    private static async Task<(TestCore Core, Guid SpaceId)> HomeAsync(IReadOnlyList<string>? places = null, bool examples = false)
    {
        var core = People();
        var spaces = core.SpaceManager();
        var owen = Id(core, "owen");
        var home = await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Home", "home", null, places, examples), default);
        await spaces.AddMemberAsync(owen, home.Id, new AddSpaceMemberRequest("eddie", SpaceRole.Editor), default);
        await spaces.AddMemberAsync(owen, home.Id, new AddSpaceMemberRequest("vera", SpaceRole.Viewer), default);
        return (core, home.Id);
    }

    private static string Names(IEnumerable<Item> items) => string.Join(", ", items.Select(item => item.Name).Order(StringComparer.Ordinal));

    [Test]
    public async Task A_kind_starts_with_its_features_unless_others_are_chosen()
    {
        var core = People();
        var owen = Id(core, "owen");
        var spaces = core.SpaceManager();

        var home = await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Home", "home"), default);
        var shop = await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Shop", "business", [" Audits", "values", "audits"]), default);
        var bare = await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Bare", "hobby", []), default);
        var none = await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("None", null), default);

        await Assert.That(string.Join(", ", home.Features)).IsEqualTo("values, barcodes, lending, reminders");
        await Assert.That(string.Join(", ", shop.Features)).IsEqualTo("values, audits");
        await Assert.That(bare.Features).IsEmpty();
        await Assert.That(none.Features).IsEmpty();
        await Assert.That(core.Spaces.Rows.Single(space => space.Id == shop.Id).Features).IsEquivalentTo(["values", "audits"]);
    }

    [Test]
    public async Task Places_are_containers_of_the_kinds_place_type_made_with_the_space()
    {
        var (core, home) = await HomeAsync(["Kitchen", " Garage ", "kitchen", "Den"]);

        var places = core.Items.Rows.Where(item => item.SpaceId == home).ToList();
        var room = core.Types.Rows.Single(type => type.SpaceId == home && type.Name == "Room");
        await Assert.That(Names(places)).IsEqualTo("Den, Garage, Kitchen");
        await Assert.That(places.All(place => place.ParentId is null && place.HoldsItems && place.TypeId == room.Id && !place.IsExample)).IsTrue();
        await Assert.That(core.Items.Events.Count(itemEvent => itemEvent.Kind == ItemEventKind.Created)).IsEqualTo(3);
        await Assert.That(core.Messages.Published.OfType<ItemsChanged>().Single().ContainerIds.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Examples_go_in_the_places_chosen_or_their_own_and_are_marked()
    {
        var (core, home) = await HomeAsync(["Garage"], examples: true);

        var rows = core.Items.Rows.Where(item => item.SpaceId == home).ToList();
        var garage = core.Items.Named("Garage");
        var toolbox = core.Items.Named("Toolbox");
        await Assert.That(garage.IsExample).IsFalse();
        await Assert.That(toolbox.ParentId).IsEqualTo(garage.Id);
        await Assert.That(core.Items.Named("Cordless drill").ParentId).IsEqualTo(toolbox.Id);
        await Assert.That(core.Items.Named("Cordless drill").Value).IsEqualTo(89m);
        await Assert.That(core.Items.Named("Rice").Quantity).IsEqualTo(2m);

        // Kitchen and Loft weren't chosen, so they were made for the examples, and are examples too.
        await Assert.That(core.Items.Named("Kitchen").IsExample).IsTrue();
        await Assert.That(Names(rows.Where(item => !item.IsExample))).IsEqualTo("Garage");
        await Assert.That(core.Items.Events.Count).IsEqualTo(rows.Count);
    }

    [Test]
    [Arguments("home")]
    [Arguments("hobby")]
    [Arguments("collection")]
    [Arguments("business")]
    public async Task Every_packs_places_and_examples_pass_the_checks(string key)
    {
        var core = People();
        var pack = core.SpaceManager().ListStarterPacks().Single(pack => pack.Key == key);

        // Every place and example goes through the same checks as an item added by hand, or this throws.
        var space = await core.SpaceManager().CreateSpaceAsync(
            Id(core, "owen"), new CreateSpaceRequest("Mine", key, null, pack.Places!.Suggest, Examples: true), default);

        var tops = core.Items.Rows.Where(item => item.SpaceId == space.Id && item.ParentId is null).Select(item => item.Name).ToHashSet();
        await Assert.That(pack.Icon).IsNotNull();
        await Assert.That(pack.Features.All(feature => SpaceFeatures.Find(feature) is not null)).IsTrue();
        await Assert.That(pack.GettingStarted.All(step => GettingStartedSteps.Find(step) is not null)).IsTrue();
        await Assert.That(pack.Hints.Keys.Order().ToList()).IsEquivalentTo(["browse", "find", "scan", "types"]);
        await Assert.That(pack.Types).Contains(pack.Places.Type);
        await Assert.That(pack.Places.Suggest.All(tops.Contains)).IsTrue();
        await Assert.That(pack.Examples).IsNotEmpty();
        await Assert.That(core.Items.Rows.Count(item => item.IsExample)).IsGreaterThanOrEqualTo(pack.Examples.Count);
    }

    [Test]
    public async Task The_collection_pack_has_its_own_types_and_hobby_no_longer_has_collectibles()
    {
        var packs = new TestCore().SpaceManager().ListStarterPacks();

        await Assert.That(string.Join(", ", packs.Single(pack => pack.Key == "collection").Types)).IsEqualTo("Display case, Binder, Set, Collectible");
        await Assert.That(packs.Single(pack => pack.Key == "hobby").Types).DoesNotContain("Collectible");
    }

    [Test]
    public async Task Unknown_features_and_kinds_are_refused()
    {
        var (core, home) = await HomeAsync();
        var owen = Id(core, "owen");
        var spaces = core.SpaceManager();

        var onCreate = await TestData.Caught<RuleViolationException>(
            () => spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Odd", "home", ["values", "teleport"]), default));
        var onSet = await TestData.Caught<RuleViolationException>(
            () => spaces.SetFeaturesAsync(owen, home, new SpaceFeaturesRequest(["teleport"]), default));
        var kind = await TestData.Caught<RuleViolationException>(
            () => spaces.SetKindAsync(owen, home, new SpaceKindRequest("garden"), default));

        await Assert.That(onCreate?.Message).IsEqualTo(
            "There is no feature called teleport. Pick from values, audits, barcodes, stock, lending, reminders, moving, projects.");
        await Assert.That(onSet?.Message).StartsWith("There is no feature called teleport.");
        await Assert.That(kind?.Message).IsEqualTo("There is no starter pack called garden. Pick one of home, hobby, collection, business, or none.");
        await Assert.That(core.Spaces.Rows.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Only_owners_switch_features_and_change_the_kind()
    {
        var (core, home) = await HomeAsync();
        var spaces = core.SpaceManager();

        var viewer = await TestData.Caught<NotAllowedException>(
            () => spaces.SetFeaturesAsync(Id(core, "vera"), home, new SpaceFeaturesRequest(["audits"]), default));
        var editor = await TestData.Caught<NotAllowedException>(
            () => spaces.SetKindAsync(Id(core, "eddie"), home, new SpaceKindRequest("hobby"), default));
        var switched = await spaces.SetFeaturesAsync(Id(core, "owen"), home, new SpaceFeaturesRequest(["audits", "values"]), default);
        var kind = await spaces.SetKindAsync(Id(core, "owen"), home, new SpaceKindRequest("Business"), default);

        await Assert.That(viewer).IsNotNull();
        await Assert.That(editor).IsNotNull();
        await Assert.That(string.Join(", ", switched.Features)).IsEqualTo("values, audits");
        await Assert.That(kind.Preset).IsEqualTo("business");

        // A new kind never changes the types.
        await Assert.That(core.Types.Rows.Any(type => type.Name == "Product")).IsFalse();
    }

    [Test]
    public async Task Removing_examples_leaves_your_own_things()
    {
        var (core, home) = await HomeAsync(["Garage"], examples: true);
        var items = core.ItemManager();
        var eddie = Id(core, "eddie");

        // A glove of Eddie's in the example toolbox keeps the toolbox; an edited example becomes his own.
        var toolbox = core.Items.Named("Toolbox");
        await items.CreateItemAsync(eddie, home, new ItemRequest("Glove", null, null, toolbox.Id, null, null, null, null, null, null, 0), default);
        var toaster = await items.GetItemAsync(eddie, home, core.Items.Named("Toaster").Id, default);
        await items.UpdateItemAsync(eddie, home, toaster.Item.Id, new ItemRequest(
            "My toaster", null, toaster.Item.TypeId, toaster.Item.ParentId, null, null, null, toaster.Item.Fields, null, null, toaster.Item.Version), default);

        var examples = core.Items.Rows.Count(item => item.IsExample);
        var refused = await TestData.Caught<NotAllowedException>(() => core.SpaceManager().RemoveExamplesAsync(Id(core, "vera"), home, default));
        var removed = await core.SpaceManager().RemoveExamplesAsync(eddie, home, default);

        await Assert.That(refused).IsNotNull();
        await Assert.That(Names(core.Items.Rows)).IsEqualTo("Garage, Glove, Kitchen, My toaster, Toolbox");
        // The toolbox and the kitchen are kept for what's in them, and stay examples.
        await Assert.That(removed.Removed).IsEqualTo(examples - 2);
        await Assert.That(Names(core.Items.Rows.Where(item => item.IsExample))).IsEqualTo("Kitchen, Toolbox");
        await Assert.That(core.Items.ExampleRemovals).IsEqualTo(1);
        await Assert.That(core.Items.Events.All(itemEvent => core.Items.Rows.Any(item => item.Id == itemEvent.ItemId))).IsTrue();

        // Garage, the toolbox and the kitchen get recounted; nothing removed is.
        var recount = core.Messages.Published.OfType<ItemsChanged>().Last().ContainerIds;
        await Assert.That(recount.All(id => core.Items.Rows.Any(item => item.Id == id))).IsTrue();
    }

    [Test]
    public async Task Getting_started_is_ticked_from_what_the_space_has()
    {
        var (core, home) = await HomeAsync(examples: true);
        var spaces = core.SpaceManager();
        var owen = Id(core, "owen");

        // Examples don't count, and Eddie and Vera were invited.
        var before = await spaces.GetGettingStartedAsync(owen, home, default);
        await Assert.That(string.Join(", ", before.Steps.Select(step => $"{step.Key}:{step.Done}")))
            .IsEqualTo("places:False, labels:False, pack:False, photo:False, lend:False, remind:False, invite:True");
        await Assert.That(before.ExamplesLeft).IsGreaterThan(0);

        var shed = await core.ItemManager().CreateItemAsync(
            owen, home, new ItemRequest("Shed", null, null, null, true, null, null, null, null, null, 0), default);
        var rake = await core.ItemManager().CreateItemAsync(
            owen, home, new ItemRequest("Rake", null, null, shed.Id, null, null, null, null, null, null, 0), default);
        core.Labels.Rows.Add(new Label { Code = "7K3Q", SpaceId = home });
        core.Attachments.Rows.Add(new Attachment { SpaceId = home, ItemId = rake.Id, Kind = AttachmentKind.Photo });
        await core.ItemManager().LendAsync(owen, home, rake.Id, new LendRequest("Sam", null), default);
        var appliance = core.Types.Rows.First(type => type.SpaceId == home && type.Fields.Any(field => field.Key == "warranty"));
        var warranty = appliance.Fields.First(field => field.Key == "warranty").Id;
        await core.ItemManager().CreateItemAsync(owen, home, new ItemRequest("Kettle", null, appliance.Id, shed.Id, null, null, null,
            new Dictionary<Guid, System.Text.Json.JsonElement> { [warranty] = System.Text.Json.JsonSerializer.SerializeToElement("2027-03-01") }, null, null, 0), default);

        var after = await spaces.GetGettingStartedAsync(owen, home, default);
        await Assert.That(after.Steps.All(step => step.Done)).IsTrue();
    }

    [Test]
    public async Task A_step_of_a_feature_that_is_off_is_left_off_the_card()
    {
        var core = People();
        var owen = Id(core, "owen");
        var spaces = core.SpaceManager();
        var on = await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Cards", "collection"), default);
        var off = await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Coins", "collection", ["lending"]), default);

        await Assert.That((await spaces.GetGettingStartedAsync(owen, on.Id, default)).Steps.Select(step => step.Key)).Contains("values");
        await Assert.That((await spaces.GetGettingStartedAsync(owen, off.Id, default)).Steps.Select(step => step.Key)).DoesNotContain("values");
    }

    [Test]
    public async Task Dismissing_the_card_changes_only_your_own()
    {
        var (core, home) = await HomeAsync();
        var spaces = core.SpaceManager();

        var dismissed = await spaces.DismissGettingStartedAsync(Id(core, "vera"), home, true, default);
        var others = await spaces.GetGettingStartedAsync(Id(core, "owen"), home, default);

        await Assert.That(dismissed.Dismissed).IsTrue();
        await Assert.That(others.Dismissed).IsFalse();
        await Assert.That((await spaces.DismissGettingStartedAsync(Id(core, "vera"), home, false, default)).Dismissed).IsFalse();
    }

    [Test]
    public async Task The_migration_gives_old_spaces_their_kinds_defaults_and_others_none()
    {
        var operations = new AddSpaceFeatures().UpOperations;

        var column = operations.OfType<AddColumnOperation>().Single(operation => operation.Name == "Features");
        var updates = operations.OfType<SqlOperation>().Select(operation => operation.Sql).ToList();
        await Assert.That(column.DefaultValueSql).IsEqualTo("'{}'");
        await Assert.That(updates).Contains(
            "UPDATE \"Spaces\" SET \"Features\" = ARRAY['values', 'barcodes', 'lending', 'reminders']::text[] WHERE \"Preset\" = 'home';");

        // The kinds that existed then get what their packs start with now.
        var packs = new TestCore().SpaceManager().ListStarterPacks();
        foreach (var (preset, features) in AddSpaceFeatures.Defaults)
        {
            await Assert.That(string.Join(",", features)).IsEqualTo(string.Join(",", packs.Single(pack => pack.Key == preset).Features));
            await Assert.That(updates.Count(sql => sql.EndsWith($"WHERE \"Preset\" = '{preset}';", StringComparison.Ordinal))).IsEqualTo(1);
        }
    }

    [Test]
    public async Task Every_feature_is_listed_and_offered()
    {
        var features = new TestCore().SpaceManager().ListFeatures();

        await Assert.That(string.Join(", ", features.Where(feature => feature.Available).Select(feature => feature.Key))).IsEqualTo("values, audits, barcodes, stock, lending, reminders, moving, projects");
        await Assert.That(features.Count).IsEqualTo(8);
    }

    [Test]
    public async Task A_type_is_copied_from_another_pack_with_its_fields()
    {
        var (core, home) = await HomeAsync();
        var types = core.TypeManager();
        var eddie = Id(core, "eddie");

        var kit = await types.CopyPackTypeAsync(eddie, home, new PackTypeRequest("hobby", " kit "), default);

        await Assert.That(kit.Name).IsEqualTo("Kit");
        await Assert.That(kit.Fields).IsNotEmpty();
        await Assert.That(kit.Version).IsEqualTo(1);
        var listed = await types.ListTypesAsync(eddie, home, default);
        await Assert.That(listed.Count(type => type.Name == "Kit")).IsEqualTo(1);
    }

    [Test]
    public async Task Copying_a_type_is_refused_for_a_viewer_a_missing_type_and_a_name_already_used()
    {
        var (core, home) = await HomeAsync();
        var types = core.TypeManager();

        await Assert.ThrowsAsync<NotAllowedException>(() => types.CopyPackTypeAsync(Id(core, "vera"), home, new PackTypeRequest("hobby", "Kit"), default));
        var missing = await Assert.ThrowsAsync<RuleViolationException>(() => types.CopyPackTypeAsync(Id(core, "owen"), home, new PackTypeRequest("hobby", "Spaceship"), default));
        await Assert.That(missing!.Message).Contains("has no type called Spaceship");
        await Assert.ThrowsAsync<ConflictException>(() => types.CopyPackTypeAsync(Id(core, "owen"), home, new PackTypeRequest("hobby", "Tool"), default));
    }
}
