using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

public class StarterPackTests
{
    [Test]
    public async Task Home_hobby_and_business_are_offered_in_order()
    {
        var packs = new TestCore().SpaceManager().ListStarterPacks();

        await Assert.That(string.Join(", ", packs.Select(pack => pack.Key))).IsEqualTo("home, hobby, business");
        await Assert.That(packs.All(pack => pack.Description.Length > 0 && pack.Types.Count > 0)).IsTrue();
    }

    [Test]
    [Arguments("home")]
    [Arguments("hobby")]
    [Arguments("business")]
    public async Task Each_pack_makes_a_space_with_its_types(string key)
    {
        var core = new TestCore();
        core.Accounts.Users.Add(new UserAccount { Username = "owen", DisplayName = "Owen" });
        var owen = core.Accounts.User("owen").Id;

        // Every type passes the same checks as one made by hand, or this throws.
        var space = await core.SpaceManager().CreateSpaceAsync(owen, new CreateSpaceRequest("Mine", key.ToUpperInvariant()), default);

        var expected = core.SpaceManager().ListStarterPacks().Single(pack => pack.Key == key).Types;
        var made = await core.TypeManager().ListTypesAsync(owen, space.Id, default);
        await Assert.That(space.Preset).IsEqualTo(key);
        await Assert.That(made.Select(type => type.Name).Order()).IsEquivalentTo(expected.Order());
        await Assert.That(made.Any(type => type.HoldsItems)).IsTrue();
        await Assert.That(made.SelectMany(type => type.Fields).All(field => field.Id is Guid id && id != Guid.Empty)).IsTrue();
    }

    [Test]
    public async Task Two_spaces_from_one_pack_get_their_own_type_and_field_ids()
    {
        var core = new TestCore();
        core.Accounts.Users.Add(new UserAccount { Username = "owen", DisplayName = "Owen" });
        var owen = core.Accounts.User("owen").Id;
        var spaces = core.SpaceManager();
        await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Home", "home"), default);
        await spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("Cabin", "home"), default);

        await Assert.That(core.Types.Rows.Select(type => type.Id).Distinct().Count()).IsEqualTo(core.Types.Rows.Count);
        var fieldIds = core.Types.Rows.SelectMany(type => type.Fields).Select(field => field.Id).ToList();
        await Assert.That(fieldIds.Distinct().Count()).IsEqualTo(fieldIds.Count);
    }

    [Test]
    public async Task No_pack_makes_an_empty_space_and_an_unknown_one_is_refused()
    {
        var core = new TestCore();
        core.Accounts.Users.Add(new UserAccount { Username = "owen", DisplayName = "Owen" });
        var owen = core.Accounts.User("owen").Id;

        var empty = await core.SpaceManager().CreateSpaceAsync(owen, new CreateSpaceRequest("Empty", " "), default);
        await Assert.That(empty.Preset).IsNull();
        await Assert.That(core.Types.Rows).IsEmpty();

        var refused = await TestData.Caught<RuleViolationException>(
            () => core.SpaceManager().CreateSpaceAsync(owen, new CreateSpaceRequest("Odd", "garden"), default));
        await Assert.That(refused?.Message).IsEqualTo("There is no starter pack called garden. Pick one of home, hobby, business, or none.");
    }
}
