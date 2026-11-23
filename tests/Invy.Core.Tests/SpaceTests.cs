using Invy.Core.Managers;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

public class SpaceTests
{
    /// <summary>A core with an admin who isn't in the space, and Owen, Eddie, Vera and Nina (not a member) as accounts.</summary>
    private static async Task<(TestCore Core, SpaceManager Spaces, Guid SpaceId)> HomeAsync()
    {
        var core = new TestCore();
        foreach (var name in new[] { "admin", "owen", "eddie", "vera", "nina" })
        {
            core.Accounts.Users.Add(new UserAccount { Username = name, DisplayName = char.ToUpperInvariant(name[0]) + name[1..], IsAdmin = name == "admin" });
        }

        var spaces = core.SpaceManager();
        var home = await spaces.CreateSpaceAsync(Id(core, "owen"), new CreateSpaceRequest("  Home ", null), default);
        await spaces.AddMemberAsync(Id(core, "owen"), home.Id, new AddSpaceMemberRequest("Eddie", SpaceRole.Editor), default);
        await spaces.AddMemberAsync(Id(core, "owen"), home.Id, new AddSpaceMemberRequest("vera", SpaceRole.Viewer), default);
        return (core, spaces, home.Id);
    }

    private static Guid Id(TestCore core, string username) => core.Accounts.User(username).Id;

    [Test]
    public async Task The_maker_owns_the_new_space()
    {
        var (core, spaces, home) = await HomeAsync();

        var mine = await spaces.ListSpacesAsync(Id(core, "owen"), default);
        await Assert.That(mine.Single().Name).IsEqualTo("Home");
        await Assert.That(mine.Single().Role).IsEqualTo(SpaceRole.Owner);

        var members = await spaces.ListMembersAsync(Id(core, "vera"), home, default);
        await Assert.That(string.Join(", ", members.Select(m => $"{m.Username}:{m.Role}"))).IsEqualTo("owen:Owner, eddie:Editor, vera:Viewer");
    }

    [Test]
    public async Task An_owner_renames_manages_members_and_deletes()
    {
        var (core, spaces, home) = await HomeAsync();
        var owen = Id(core, "owen");

        await Assert.That((await spaces.RenameSpaceAsync(owen, home, new RenameSpaceRequest("House"), default)).Name).IsEqualTo("House");
        await spaces.SetMemberRoleAsync(owen, home, Id(core, "vera"), new SpaceMemberRoleRequest(SpaceRole.Editor), default);
        var members = await spaces.RemoveMemberAsync(owen, home, Id(core, "eddie"), default);
        await Assert.That(string.Join(", ", members.Select(m => $"{m.Username}:{m.Role}"))).IsEqualTo("owen:Owner, vera:Editor");

        await spaces.DeleteSpaceAsync(owen, home, default);
        await Assert.That(core.Spaces.Rows).IsEmpty();
    }

    [Test]
    public async Task An_editor_can_look_but_not_manage_the_space()
    {
        var (core, spaces, home) = await HomeAsync();
        var eddie = Id(core, "eddie");

        await Assert.That((await spaces.ListMembersAsync(eddie, home, default)).Count).IsEqualTo(3);
        await Assert.That(await TestData.Caught<NotAllowedException>(() => spaces.RenameSpaceAsync(eddie, home, new RenameSpaceRequest("Mine"), default))).IsNotNull();
        await Assert.That(await TestData.Caught<NotAllowedException>(() => spaces.AddMemberAsync(eddie, home, new AddSpaceMemberRequest("nina", SpaceRole.Viewer), default))).IsNotNull();
        await Assert.That(await TestData.Caught<NotAllowedException>(() => spaces.DeleteSpaceAsync(eddie, home, default))).IsNotNull();
    }

    [Test]
    public async Task A_viewer_can_look_and_leave_but_not_change_roles()
    {
        var (core, spaces, home) = await HomeAsync();
        var vera = Id(core, "vera");

        var refused = await TestData.Caught<NotAllowedException>(
            () => spaces.SetMemberRoleAsync(vera, home, vera, new SpaceMemberRoleRequest(SpaceRole.Owner), default));
        await Assert.That(refused!.Message).IsEqualTo("Only an owner of Home can do that. Ask one of its owners.");
        await Assert.That(await TestData.Caught<NotAllowedException>(() => spaces.RemoveMemberAsync(vera, home, Id(core, "eddie"), default))).IsNotNull();

        await spaces.RemoveMemberAsync(vera, home, vera, default);
        await Assert.That(await spaces.ListSpacesAsync(vera, default)).IsEmpty();
    }

    [Test]
    public async Task Someone_outside_the_space_and_the_install_admin_are_told_it_is_not_there()
    {
        var (core, spaces, home) = await HomeAsync();

        foreach (var outsider in new[] { "nina", "admin" })
        {
            var refused = await TestData.Caught<NotFoundException>(() => spaces.ListMembersAsync(Id(core, outsider), home, default));
            await Assert.That(refused).IsNotNull();
        }

        await Assert.That(await TestData.Caught<NotFoundException>(() => spaces.ListMembersAsync(Id(core, "owen"), Guid.NewGuid(), default))).IsNotNull();
    }

    [Test]
    public async Task A_space_always_keeps_an_owner()
    {
        var (core, spaces, home) = await HomeAsync();
        var owen = Id(core, "owen");

        await Assert.That(await TestData.Caught<RuleViolationException>(() => spaces.RemoveMemberAsync(owen, home, owen, default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => spaces.SetMemberRoleAsync(owen, home, owen, new SpaceMemberRoleRequest(SpaceRole.Editor), default))).IsNotNull();

        await spaces.SetMemberRoleAsync(owen, home, Id(core, "eddie"), new SpaceMemberRoleRequest(SpaceRole.Owner), default);
        await spaces.RemoveMemberAsync(owen, home, owen, default);
        await Assert.That((await spaces.ListMembersAsync(Id(core, "eddie"), home, default)).Count).IsEqualTo(2);
    }

    [Test]
    public async Task Adding_checks_the_name_the_account_and_membership()
    {
        var (core, spaces, home) = await HomeAsync();
        var owen = Id(core, "owen");

        await Assert.That(await TestData.Caught<RuleViolationException>(() => spaces.CreateSpaceAsync(owen, new CreateSpaceRequest("  ", null), default))).IsNotNull();
        await Assert.That(await TestData.Caught<NotFoundException>(() => spaces.AddMemberAsync(owen, home, new AddSpaceMemberRequest("nobody", SpaceRole.Viewer), default))).IsNotNull();
        await Assert.That(await TestData.Caught<ConflictException>(() => spaces.AddMemberAsync(owen, home, new AddSpaceMemberRequest("vera", SpaceRole.Editor), default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => spaces.AddMemberAsync(owen, home, new AddSpaceMemberRequest("nina", (SpaceRole)9), default))).IsNotNull();
    }
}
