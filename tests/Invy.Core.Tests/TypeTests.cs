using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

public class TypeTests
{
    private static readonly FieldDefinition Brand = new(null, "Brand", " Brand ", FieldKind.Text, null, [], false, true);

    private static readonly FieldDefinition Price = new(null, "price", "Price", FieldKind.Money, "USD", [], false, true);

    private static readonly FieldDefinition Condition = new(null, "condition", "Condition", FieldKind.Select, null, [" New", "Used "], true, false);

    private static ItemTypeRequest Tool(params FieldDefinition[] fields) => new(" Tool ", "wrench", "#3b82f6", false, fields, 0);

    /// <summary>A space owned by Owen, with Vera as a viewer and Eddie as an editor.</summary>
    private static async Task<(TestCore Core, Guid SpaceId, Guid Owen, Guid Eddie, Guid Vera)> HomeAsync()
    {
        var core = new TestCore();
        foreach (var name in new[] { "owen", "eddie", "vera" })
        {
            core.Accounts.Users.Add(new UserAccount { Username = name, DisplayName = name });
        }

        Guid Id(string name) => core.Accounts.User(name).Id;
        var spaces = core.SpaceManager();
        var home = await spaces.CreateSpaceAsync(Id("owen"), new CreateSpaceRequest("Home", null), default);
        await spaces.AddMemberAsync(Id("owen"), home.Id, new AddSpaceMemberRequest("eddie", SpaceRole.Editor), default);
        await spaces.AddMemberAsync(Id("owen"), home.Id, new AddSpaceMemberRequest("vera", SpaceRole.Viewer), default);
        return (core, home.Id, Id("owen"), Id("eddie"), Id("vera"));
    }

    [Test]
    public async Task An_editor_adds_a_type_with_tidied_fields()
    {
        var (core, home, _, eddie, vera) = await HomeAsync();

        var tool = await core.TypeManager().CreateTypeAsync(eddie, home, Tool(Brand, Price, Condition), default);

        await Assert.That(tool.Name).IsEqualTo("Tool");
        await Assert.That(tool.Version).IsEqualTo(1);
        await Assert.That(string.Join(", ", tool.Fields.Select(f => $"{f.Key}:{f.Label}:{f.Kind}"))).IsEqualTo("brand:Brand:Text, price:Price:Money, condition:Condition:Select");
        await Assert.That(string.Join(", ", tool.Fields[2].Options)).IsEqualTo("New, Used");
        await Assert.That(tool.Fields.All(f => f.Id is Guid id && id != Guid.Empty)).IsTrue();

        var listed = await core.TypeManager().ListTypesAsync(vera, home, default);
        await Assert.That(listed.Single().Id).IsEqualTo(tool.Id);
    }

    [Test]
    public async Task Renaming_a_field_keeps_its_id_and_the_version_goes_up()
    {
        var (core, home, owen, _, _) = await HomeAsync();
        var types = core.TypeManager();
        var tool = await types.CreateTypeAsync(owen, home, Tool(Brand), default);

        var renamed = tool.Fields[0] with { Label = "Make", Key = "make" };
        var saved = await types.UpdateTypeAsync(owen, home, tool.Id, Tool(renamed) with { Version = tool.Version }, default);

        await Assert.That(saved.Fields[0].Id).IsEqualTo(tool.Fields[0].Id);
        await Assert.That(saved.Fields[0].Key).IsEqualTo("make");
        await Assert.That(saved.Version).IsEqualTo(2);
    }

    [Test]
    public async Task Saving_an_old_version_is_refused()
    {
        var (core, home, owen, eddie, _) = await HomeAsync();
        var types = core.TypeManager();
        var tool = await types.CreateTypeAsync(owen, home, Tool(Brand), default);
        await types.UpdateTypeAsync(eddie, home, tool.Id, Tool(Brand, Price) with { Version = 1 }, default);

        var refused = await TestData.Caught<ConflictException>(() => types.UpdateTypeAsync(owen, home, tool.Id, Tool() with { Version = 1 }, default));
        await Assert.That(refused).IsNotNull();
    }

    [Test]
    public async Task A_viewer_can_look_but_not_change_types()
    {
        var (core, home, owen, _, vera) = await HomeAsync();
        var types = core.TypeManager();
        var tool = await types.CreateTypeAsync(owen, home, Tool(), default);

        await Assert.That((await types.GetTypeAsync(vera, home, tool.Id, default)).Name).IsEqualTo("Tool");
        await Assert.That(await TestData.Caught<NotAllowedException>(() => types.CreateTypeAsync(vera, home, Tool() with { Name = "Box" }, default))).IsNotNull();
        await Assert.That(await TestData.Caught<NotAllowedException>(() => types.DeleteTypeAsync(vera, home, tool.Id, default))).IsNotNull();
    }

    [Test]
    public async Task Types_stay_in_their_space()
    {
        var (core, home, owen, _, _) = await HomeAsync();
        var types = core.TypeManager();
        var tool = await types.CreateTypeAsync(owen, home, Tool(), default);
        var shop = await core.SpaceManager().CreateSpaceAsync(owen, new CreateSpaceRequest("Shop", null), default);

        await Assert.That(await TestData.Caught<NotFoundException>(() => types.GetTypeAsync(owen, shop.Id, tool.Id, default))).IsNotNull();
        await Assert.That((await types.CreateTypeAsync(owen, shop.Id, Tool(), default)).Name).IsEqualTo("Tool");
        await Assert.That(await TestData.Caught<ConflictException>(() => types.CreateTypeAsync(owen, home, Tool() with { Name = "TOOL" }, default))).IsNotNull();
    }

    [Test]
    [Arguments("price", "Price", FieldKind.Number, null, new string[0], true, "")]
    [Arguments("2nd", "Second", FieldKind.Text, null, new string[0], false, "start with a letter")]
    [Arguments("type", "Type", FieldKind.Text, null, new string[0], false, "Search already uses type:")]
    [Arguments("size", "Size", FieldKind.Select, null, new string[0], false, "needs at least one option")]
    [Arguments("size", "Size", FieldKind.Select, null, new[] { "S", "s" }, false, "the option S twice")]
    [Arguments("brand", "Brand", FieldKind.Text, null, new[] { "A" }, false, "Only select fields have options")]
    [Arguments("brand", "Brand", FieldKind.Text, "kg", new string[0], false, "Only number and money fields have a unit")]
    [Arguments("notes2", "Notes", FieldKind.LongText, null, new string[0], true, "can't be sorted on")]
    [Arguments("x", " ", FieldKind.Text, null, new string[0], false, "needs a name")]
    public async Task Field_rules(string key, string label, FieldKind kind, string? unit, string[] options, bool sortable, string problem)
    {
        var (core, home, owen, _, _) = await HomeAsync();
        var field = new FieldDefinition(null, key, label, kind, unit, options, false, sortable);

        var refused = await TestData.Caught<RuleViolationException>(() => core.TypeManager().CreateTypeAsync(owen, home, Tool(field), default));

        if (problem.Length == 0)
        {
            await Assert.That(refused).IsNull();
        }
        else
        {
            await Assert.That(refused?.Message).Contains(problem);
        }
    }

    [Test]
    public async Task Two_fields_cannot_share_a_search_name()
    {
        var (core, home, owen, _, _) = await HomeAsync();

        var refused = await TestData.Caught<RuleViolationException>(
            () => core.TypeManager().CreateTypeAsync(owen, home, Tool(Brand, Brand with { Label = "Maker" }), default));

        await Assert.That(refused?.Message).IsEqualTo("Two fields are called brand. Give each field its own search name.");
    }
}
