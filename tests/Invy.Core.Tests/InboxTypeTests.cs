using Invy.Core.Engines.Transformation;
using Invy.Core.Managers;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>SuggestType: the background sweep that suggests a type for Inbox items, and accepting or ignoring one.</summary>
public class InboxTypeTests
{
    [Test]
    public async Task Things_landing_in_the_inbox_are_asked_about_shortly_after_once_a_model_is_set_up()
    {
        var space = await ItemSpace.MakeAsync();
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new("baseball", null), default);
        await Assert.That(space.Core.Messages.Scheduled.Select(job => job.Message).OfType<SuggestInboxTypes>()).IsEmpty();

        await AiTestSetup.UseClaudeAsync(space.Core);
        var box = await space.BoxAsync("Box F");
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new("glove, 2 bats", null), default);
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new("cap", box.Id), default);

        var scheduled = space.Core.Messages.Scheduled.Where(job => job.Message is SuggestInboxTypes).ToList();
        await Assert.That(scheduled.Count).IsEqualTo(1);
        await Assert.That(scheduled[0].Message).IsEqualTo(new SuggestInboxTypes(space.SpaceId));
        await Assert.That(scheduled[0].At).IsEqualTo(space.Core.Clock.GetUtcNow() + ItemManager.SuggestTypesAfter);
    }

    [Test]
    public async Task A_sweep_keeps_each_suggestion_and_asks_about_each_item_once()
    {
        var space = await ItemSpace.MakeAsync();
        await AiTestSetup.UseClaudeAsync(space.Core);
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new("hammer, sock", null), default);
        var hammer = space.Core.Items.Named("Hammer");
        var sock = space.Core.Items.Named("Sock");
        space.Core.Ai.Answers[SuggestTypePrompt.SchemaName] =
            $$"""{"suggestions":[{"itemId":"{{hammer.Id}}","typeId":"{{space.Tool.Id}}"},{"itemId":"{{sock.Id}}","typeId":""}]}""";

        await space.Items.SweepInboxTypesAsync(new(space.SpaceId), default);
        await space.Items.SweepInboxTypesAsync(new(space.SpaceId), default);

        var inbox = (await space.Items.ListInboxAsync(space.Owen, space.SpaceId, null, default)).Items.OrderBy(item => item.Name).ToList();
        await Assert.That(inbox.Select(item => item.SuggestedTypeId)).IsEquivalentTo([(Guid?)space.Tool.Id, null]);
        await Assert.That(space.Core.Ai.Asked.Count).IsEqualTo(1);
        await Assert.That(space.Core.Ai.Asked[0].Question.Text).Contains("Hammer");
        await Assert.That(space.Core.Messages.Published.OfType<InboxTypesSuggested>().Count()).IsEqualTo(1);
        await Assert.That(space.Core.Items.Named("Hammer").Version).IsEqualTo(hammer.Version);
    }

    [Test]
    public async Task A_sweep_does_nothing_without_a_model_or_types()
    {
        var space = await ItemSpace.MakeAsync();
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new("hammer", null), default);

        await space.Items.SweepInboxTypesAsync(new(space.SpaceId), default);

        await Assert.That(space.Core.Ai.Asked).IsEmpty();
        await Assert.That(space.Core.Items.Named("Hammer").TypeSuggestedAt).IsNull();
    }

    [Test]
    public async Task A_full_batch_queues_the_next_one()
    {
        var space = await ItemSpace.MakeAsync();
        await AiTestSetup.UseClaudeAsync(space.Core);
        var names = string.Join(", ", Enumerable.Range(1, ItemManager.SuggestTypeBatch + 1).Select(number => $"thing {number}"));
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new(names, null), default);
        space.Core.Ai.Answers[SuggestTypePrompt.SchemaName] = """{"suggestions":[]}""";

        await space.Items.SweepInboxTypesAsync(new(space.SpaceId), default);
        await Assert.That(space.Core.Messages.Published.OfType<SuggestInboxTypes>().Count()).IsEqualTo(1);

        await space.Items.SweepInboxTypesAsync(new(space.SpaceId), default);
        await Assert.That(space.Core.Messages.Published.OfType<SuggestInboxTypes>().Count()).IsEqualTo(1);
        await Assert.That(space.Core.Items.Rows.All(item => item.TypeSuggestedAt is not null)).IsTrue();
    }

    [Test]
    public async Task Accepting_gives_the_type_with_a_changed_event()
    {
        var space = await ItemSpace.MakeAsync();
        var hammer = await SuggestedAsync(space, "hammer");

        var accepted = await space.Items.AcceptSuggestedTypeAsync(space.Owen, space.SpaceId, hammer.Id, default);

        await Assert.That(accepted.TypeId).IsEqualTo(space.Tool.Id);
        await Assert.That(accepted.SuggestedTypeId).IsNull();
        await Assert.That(space.Core.Items.Events.Count(e => e.ItemId == hammer.Id && e.Kind == ItemEventKind.Changed)).IsEqualTo(1);
        await Assert.That((await space.Items.ListInboxAsync(space.Owen, space.SpaceId, null, default)).Items).IsEmpty();
    }

    [Test]
    public async Task Ignoring_drops_the_suggestion_until_someone_asks_again()
    {
        var space = await ItemSpace.MakeAsync();
        var hammer = await SuggestedAsync(space, "hammer");

        var ignored = await space.Items.IgnoreSuggestedTypeAsync(space.Owen, space.SpaceId, hammer.Id, default);
        await space.Items.SweepInboxTypesAsync(new(space.SpaceId), default);
        var noSuggestion = await TestData.Caught<RuleViolationException>(() => space.Items.AcceptSuggestedTypeAsync(space.Owen, space.SpaceId, hammer.Id, default));

        await Assert.That(ignored.SuggestedTypeId).IsNull();
        await Assert.That(ignored.TypeId).IsNull();
        await Assert.That(space.Core.Ai.Asked.Count).IsEqualTo(1);
        await Assert.That(noSuggestion!.Message).Contains("no suggested type");

        space.Core.Clock.Now += TimeSpan.FromMinutes(1);
        await space.Items.SuggestInboxTypesAsync(space.Owen, space.SpaceId, default);
        var again = space.Core.Messages.Published.OfType<SuggestInboxTypes>().Single();
        await space.Items.SweepInboxTypesAsync(again, default);

        await Assert.That(space.Core.Ai.Asked.Count).IsEqualTo(2);
        await Assert.That(space.Core.Items.Named("Hammer").SuggestedTypeId).IsEqualTo(space.Tool.Id);
    }

    [Test]
    public async Task Asking_again_needs_an_editor_a_model_and_types()
    {
        var space = await ItemSpace.MakeAsync();

        var noModel = await TestData.Caught<RuleViolationException>(() => space.Items.SuggestInboxTypesAsync(space.Owen, space.SpaceId, default));
        await AiTestSetup.UseClaudeAsync(space.Core);
        var viewer = await TestData.Caught<NotAllowedException>(() => space.Items.SuggestInboxTypesAsync(space.Vera, space.SpaceId, default));

        await Assert.That(noModel!.Message).Contains("AI assistants");
        await Assert.That(viewer).IsNotNull();
        await Assert.That(space.Core.Messages.Published.OfType<SuggestInboxTypes>()).IsEmpty();
    }

    [Test]
    public async Task Ids_the_model_made_up_read_as_no_suggestion()
    {
        var space = await ItemSpace.MakeAsync();
        await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new("hammer", null), default);
        var hammer = space.Core.Items.Named("Hammer");
        var types = space.Core.Types.Rows.ToList();

        var read = TransformationEngine.ReadInboxTypes(
            $$"""{"suggestions":[{"itemId":"{{Guid.NewGuid()}}","typeId":"{{space.Tool.Id}}"},{"itemId":"{{hammer.Id}}","typeId":"{{Guid.NewGuid()}}"}]}""",
            [hammer], types);

        await Assert.That(read).IsEquivalentTo(new Dictionary<Guid, Guid?> { [hammer.Id] = null });
        await Assert.That(TransformationEngine.ReadInboxTypes("{}", [hammer], types)[hammer.Id]).IsNull();
    }

    /// <summary>Adds an Inbox item and has the model suggest the Tool type for it.</summary>
    private static async Task<ItemResponse> SuggestedAsync(ItemSpace space, string name)
    {
        await AiTestSetup.UseClaudeAsync(space.Core);
        var made = (await space.Items.QuickAddAsync(space.Owen, space.SpaceId, new(name, null), default)).Single();
        space.Core.Ai.Answers[SuggestTypePrompt.SchemaName] = $$"""{"suggestions":[{"itemId":"{{made.Id}}","typeId":"{{space.Tool.Id}}"}]}""";
        await space.Items.SweepInboxTypesAsync(new(space.SpaceId), default);
        return made;
    }
}
