using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

public class LabelTests
{
    private static readonly LabelEngine Engine = new();

    [Test]
    public async Task A_batch_makes_distinct_unclaimed_codes_without_look_alikes()
    {
        var space = await ItemSpace.MakeAsync();

        var made = await space.Core.LabelManager().CreateLabelsAsync(space.Owen, space.SpaceId, new CreateLabelsRequest(65), default);

        await Assert.That(made.Count).IsEqualTo(65);
        await Assert.That(made.Select(label => label.Code).Distinct().Count()).IsEqualTo(65);
        await Assert.That(made.All(label => label.Code.Length == 4 && label.Code.All(LabelEngine.Alphabet.Contains))).IsTrue();
        await Assert.That(made.All(label => label.Item is null && label.SpaceId == space.SpaceId)).IsTrue();
        await Assert.That(space.Core.Labels.Rows.Count).IsEqualTo(65);
        await Assert.That(LabelEngine.Alphabet.Intersect("0O1IL5S8B2Z").Any()).IsFalse();
    }

    [Test]
    public async Task Codes_get_longer_as_the_install_fills_up()
    {
        await Assert.That(Engine.LengthFor(0)).IsEqualTo(4);
        await Assert.That(Engine.LengthFor(19_999)).IsEqualTo(4);
        await Assert.That(Engine.LengthFor(20_000)).IsEqualTo(5);
        await Assert.That(Engine.LengthFor(500_000)).IsEqualTo(6);
    }

    [Test]
    [Arguments(0)]
    [Arguments(501)]
    public async Task A_batch_size_out_of_range_is_refused(int count)
    {
        var space = await ItemSpace.MakeAsync();

        var error = await TestData.Caught<RuleViolationException>(
            () => space.Core.LabelManager().CreateLabelsAsync(space.Owen, space.SpaceId, new CreateLabelsRequest(count), default));

        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task Viewers_cant_make_labels()
    {
        var space = await ItemSpace.MakeAsync();

        var error = await TestData.Caught<NotAllowedException>(
            () => space.Core.LabelManager().CreateLabelsAsync(space.Vera, space.SpaceId, new CreateLabelsRequest(1), default));

        await Assert.That(error).IsNotNull();
    }

    [Test]
    [Arguments("7K3Q", "7K3Q")]
    [Arguments(" 7k3q ", "7K3Q")]
    [Arguments("https://invy.local/l/7K3Q", "7K3Q")]
    [Arguments("https://invy.local:8443/l/7k3qa/?x=1", "7K3QA")]
    [Arguments("https:%2F%2Finvy.local%2Fl%2F7K3Q", "7K3Q")]
    public async Task A_code_is_read_from_what_was_scanned_or_typed(string text, string code)
    {
        await Assert.That(Engine.Code(text)).IsEqualTo(code);
    }

    [Test]
    [Arguments("")]
    [Arguments("7K3")]
    [Arguments("7K3Q7K3")]
    [Arguments("BOX1")]
    [Arguments("https://example.com/")]
    public async Task Text_that_isnt_a_code_is_refused(string text)
    {
        await Assert.That(await TestData.Caught<RuleViolationException>(() => Task.FromResult(Engine.Code(text)))).IsNotNull();
    }

    [Test]
    public async Task Claiming_a_label_makes_a_box_in_the_place_named()
    {
        var space = await ItemSpace.MakeAsync();
        var shed = await space.BoxAsync("Shed");
        var code = await LabelAsync(space);

        var box = await space.Core.LabelManager().ClaimLabelAsync(space.Owen, space.SpaceId, code, new ClaimLabelRequest(shed.Id, null, null), default);

        await Assert.That(box.Name).IsEqualTo($"Box {code}");
        await Assert.That(box.HoldsItems).IsTrue();
        await Assert.That(box.ParentId).IsEqualTo(shed.Id);
        await Assert.That(box.LabelCode).IsEqualTo(code);
        await Assert.That(space.Core.Labels.Code(code).ItemId).IsEqualTo(box.Id);
        var created = space.Core.Items.Events.Single(e => e.ItemId == box.Id);
        await Assert.That(created.Kind).IsEqualTo(ItemEventKind.Created);
        await Assert.That(created.ToParent).IsEqualTo(shed.Id);
        await Assert.That(space.Core.Messages.Published.OfType<ItemsChanged>().Any(m => m.ContainerIds.Contains(shed.Id))).IsTrue();
    }

    [Test]
    public async Task Claiming_with_a_name_and_type_uses_them()
    {
        var space = await ItemSpace.MakeAsync();
        var code = await LabelAsync(space);

        var drill = await space.Core.LabelManager().ClaimLabelAsync(
            space.Owen, space.SpaceId, code.ToLowerInvariant(), new ClaimLabelRequest(null, "Drill", space.Tool.Id), default);

        await Assert.That(drill.Name).IsEqualTo("Drill");
        await Assert.That(drill.TypeId).IsEqualTo(space.Tool.Id);
        await Assert.That(drill.HoldsItems).IsFalse();
        await Assert.That(drill.LabelCode).IsEqualTo(code);
    }

    [Test]
    public async Task A_claimed_label_cant_be_claimed_again()
    {
        var space = await ItemSpace.MakeAsync();
        var code = await LabelAsync(space);
        var labels = space.Core.LabelManager();
        await labels.ClaimLabelAsync(space.Owen, space.SpaceId, code, new ClaimLabelRequest(null, null, null), default);

        var error = await TestData.Caught<RuleViolationException>(
            () => labels.ClaimLabelAsync(space.Owen, space.SpaceId, code, new ClaimLabelRequest(null, null, null), default));

        await Assert.That(error!.Message).Contains($"already on Box {code}");
    }

    [Test]
    public async Task Finding_a_label_names_its_space_and_item()
    {
        var space = await ItemSpace.MakeAsync();
        var code = await LabelAsync(space);
        var labels = space.Core.LabelManager();

        var unclaimed = await labels.FindLabelAsync(space.Vera, $"https://invy.local/l/{code}", default);
        var box = await labels.ClaimLabelAsync(space.Owen, space.SpaceId, code, new ClaimLabelRequest(null, null, null), default);
        var claimed = await labels.FindLabelAsync(space.Vera, code, default);

        await Assert.That(unclaimed.SpaceId).IsEqualTo(space.SpaceId);
        await Assert.That(unclaimed.Item).IsNull();
        await Assert.That(claimed.Item!.Id).IsEqualTo(box.Id);
    }

    [Test]
    public async Task Someone_outside_the_space_cant_find_its_labels()
    {
        var space = await ItemSpace.MakeAsync();
        var code = await LabelAsync(space);
        space.Core.Accounts.Users.Add(new UserAccount { Username = "olga", DisplayName = "Olga" });
        var olga = space.Core.Accounts.User("olga").Id;

        var error = await TestData.Caught<NotFoundException>(() => space.Core.LabelManager().FindLabelAsync(olga, code, default));

        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task A_label_cant_be_claimed_through_another_space()
    {
        var space = await ItemSpace.MakeAsync();
        var code = await LabelAsync(space);
        var other = await space.Core.SpaceManager().CreateSpaceAsync(space.Owen, new CreateSpaceRequest("Work", null), default);

        var error = await TestData.Caught<NotFoundException>(
            () => space.Core.LabelManager().ClaimLabelAsync(space.Owen, other.Id, code, new ClaimLabelRequest(null, null, null), default));

        await Assert.That(error!.Message).Contains("another space");
    }

    [Test]
    public async Task Attaching_a_label_to_an_item_frees_its_old_one()
    {
        var space = await ItemSpace.MakeAsync();
        var ball = await space.ThingAsync("Baseball");
        var first = await LabelAsync(space);
        var second = await LabelAsync(space);
        var labels = space.Core.LabelManager();

        await labels.AttachLabelAsync(space.Owen, space.SpaceId, first, new AttachLabelRequest(ball.Id), default);
        var moved = await labels.AttachLabelAsync(space.Owen, space.SpaceId, second, new AttachLabelRequest(ball.Id), default);

        await Assert.That(moved.LabelCode).IsEqualTo(second);
        await Assert.That(moved.Version).IsEqualTo(ball.Version + 2);
        await Assert.That(space.Core.Labels.Code(first).ItemId).IsNull();
        await Assert.That(space.Core.Labels.Code(second).ItemId).IsEqualTo(ball.Id);
        var change = space.Core.Items.Events.Last(e => e.ItemId == ball.Id && e.Kind == ItemEventKind.Changed).Changes.Single();
        await Assert.That(change).IsEqualTo(new ItemChange("Label", first, second));
    }

    [Test]
    public async Task Attaching_a_label_thats_on_another_item_is_refused()
    {
        var space = await ItemSpace.MakeAsync();
        var ball = await space.ThingAsync("Baseball");
        var glove = await space.ThingAsync("Glove");
        var code = await LabelAsync(space);
        var labels = space.Core.LabelManager();
        await labels.AttachLabelAsync(space.Owen, space.SpaceId, code, new AttachLabelRequest(ball.Id), default);

        var again = await labels.AttachLabelAsync(space.Owen, space.SpaceId, code, new AttachLabelRequest(ball.Id), default);
        var error = await TestData.Caught<RuleViolationException>(
            () => labels.AttachLabelAsync(space.Owen, space.SpaceId, code, new AttachLabelRequest(glove.Id), default));

        await Assert.That(again.Version).IsEqualTo(ball.Version + 1);
        await Assert.That(error!.Message).Contains("already on Baseball");
    }

    [Test]
    public async Task Listing_unclaimed_labels_leaves_out_claimed_ones()
    {
        var space = await ItemSpace.MakeAsync();
        var labels = space.Core.LabelManager();
        var made = await labels.CreateLabelsAsync(space.Owen, space.SpaceId, new CreateLabelsRequest(3), default);
        await labels.ClaimLabelAsync(space.Owen, space.SpaceId, made[0].Code, new ClaimLabelRequest(null, null, null), default);

        var all = await labels.ListLabelsAsync(space.Vera, space.SpaceId, false, null, default);
        var unclaimed = await labels.ListLabelsAsync(space.Vera, space.SpaceId, true, null, default);

        await Assert.That(all.Labels.Count).IsEqualTo(3);
        await Assert.That(all.Labels.Single(label => label.Code == made[0].Code).Item!.Name).IsEqualTo($"Box {made[0].Code}");
        await Assert.That(string.Join(", ", unclaimed.Labels.Select(label => label.Code))).IsEqualTo($"{made[1].Code}, {made[2].Code}");
    }

    [Test]
    public async Task Search_finds_an_item_by_its_label_code()
    {
        var space = await ItemSpace.MakeAsync();
        var code = await LabelAsync(space);
        var box = await space.Core.LabelManager().ClaimLabelAsync(space.Owen, space.SpaceId, code, new ClaimLabelRequest(null, "Tools", null), default);
        await space.ThingAsync("Hammer", box);

        var byCode = await space.Items.SearchAsync(space.Vera, space.SpaceId, code, null, null, default);
        var inside = await space.Items.SearchAsync(space.Vera, space.SpaceId, $"in:{code}", null, null, default);

        await Assert.That(string.Join(", ", byCode.Results.Select(hit => hit.Item.Name))).IsEqualTo("Tools");
        await Assert.That(string.Join(", ", inside.Results.Select(hit => hit.Item.Name))).IsEqualTo("Hammer");
    }

    private static async Task<string> LabelAsync(ItemSpace space) =>
        (await space.Core.LabelManager().CreateLabelsAsync(space.Owen, space.SpaceId, new CreateLabelsRequest(1), default))[0].Code;
}
