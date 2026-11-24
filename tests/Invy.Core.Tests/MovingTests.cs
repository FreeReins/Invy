using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>Moving mode: destinations, arriving, and the manifest.</summary>
public class MovingTests
{
    [Test]
    public async Task A_box_gets_a_destination_and_arrives_with_one_move()
    {
        var space = await ItemSpace.MakeAsync();
        var house = await space.BoxAsync("New house");
        var kitchen = await space.BoxAsync("Kitchen", house);
        var box = await space.BoxAsync("Box 1");
        await space.ThingAsync("Plates", box);

        await space.Items.SetDestinationAsync(space.Owen, space.SpaceId, box.Id, new DestinationRequest(kitchen.Id), default);
        var going = await space.Items.SearchAsync(space.Owen, space.SpaceId, "going:kitchen", null, null, default);
        var moves = space.Core.Items.Events.Count(e => e.Kind == ItemEventKind.Moved);
        var arrived = await space.Items.MarkArrivedAsync(space.Owen, space.SpaceId, box.Id, default);

        await Assert.That(string.Join(", ", going.Results.Select(hit => hit.Item.Name))).IsEqualTo("Box 1");
        await Assert.That(arrived.ParentId).IsEqualTo(kitchen.Id);
        await Assert.That(arrived.DestinationId).IsNull();
        await Assert.That(space.Core.Items.Events.Count(e => e.Kind == ItemEventKind.Moved) - moves).IsEqualTo(1);
        await Assert.That(space.Core.Items.Named("Plates").Path).StartsWith(space.Core.Items.Named("New house").Path);
    }

    [Test]
    public async Task A_box_cant_be_going_into_itself()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box 1");
        var inner = await space.BoxAsync("Inner", box);

        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.SetDestinationAsync(space.Owen, space.SpaceId, box.Id, new DestinationRequest(inner.Id), default))).IsNotNull();
    }

    [Test]
    public async Task The_manifest_lists_the_destination_and_what_is_inside()
    {
        var space = await ItemSpace.MakeAsync();
        var house = await space.BoxAsync("New house");
        var kitchen = await space.BoxAsync("Kitchen", house);
        var box = await space.BoxAsync("Box 1");
        await space.ThingAsync("Plates", box);
        await space.ThingAsync("Cups", box, quantity: 6);
        await space.Items.SetDestinationAsync(space.Owen, space.SpaceId, box.Id, new DestinationRequest(kitchen.Id), default);

        var manifest = await space.Items.GetManifestAsync(space.Vera, space.SpaceId, box.Id, default);

        await Assert.That(string.Join(" › ", manifest.DestinationPath.Select(place => place.Name))).IsEqualTo("New house › Kitchen");
        await Assert.That(string.Join(", ", manifest.Contents.Select(item => item.Name))).IsEqualTo("Cups, Plates");
    }

    [Test]
    public async Task Arriving_without_a_destination_is_refused()
    {
        var space = await ItemSpace.MakeAsync();
        var box = await space.BoxAsync("Box 1");

        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => space.Items.MarkArrivedAsync(space.Owen, space.SpaceId, box.Id, default))).IsNotNull();
    }
}
