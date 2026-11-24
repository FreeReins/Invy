using Invy.Core.Engines.Transformation;
using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>ItemsFromPhoto and ItemDetailsFromPhoto: reading the model's answers, and that nothing is saved from them.</summary>
public class ItemPhotoTests
{
    [Test]
    public async Task A_good_answer_becomes_typed_suggestions_with_fitting_values()
    {
        var space = await ItemSpace.MakeAsync();
        var json = $$"""
            {"items":[
              {"name":"Cordless drill","typeId":"{{space.Tool.Id}}","quantity":0,"unit":"","fields":[
                {"fieldId":"{{space.Field("brand")}}","value":"DeWalt"},
                {"fieldId":"{{space.Field("price")}}","value":"$129.99"},
                {"fieldId":"{{space.Field("power")}}","value":"cordless"},
                {"fieldId":"{{space.Field("bought")}}","value":"2024-03-01"}]},
              {"name":"AA batteries","typeId":"","quantity":12,"unit":"","fields":[]},
              {"name":"Wood glue","typeId":"","quantity":0.5,"unit":"l","fields":[]}
            ]}
            """;

        var read = TransformationEngine.ReadItemSuggestions(json, Types(space));

        await Assert.That(read.Select(item => item.Name)).IsEquivalentTo(["Cordless drill", "AA batteries", "Wood glue"]);
        var drill = read[0];
        await Assert.That(drill.TypeId).IsEqualTo(space.Tool.Id);
        await Assert.That(drill.Quantity).IsNull();
        await Assert.That(drill.Fields[space.Field("brand")].GetString()).IsEqualTo("DeWalt");
        await Assert.That(drill.Fields[space.Field("price")].GetDecimal()).IsEqualTo(129.99m);
        await Assert.That(drill.Fields[space.Field("power")].GetString()).IsEqualTo("Cordless");
        await Assert.That(drill.Fields[space.Field("bought")].GetString()).IsEqualTo("2024-03-01");
        await Assert.That(read[1].Quantity).IsEqualTo(12m);
        await Assert.That(read[1].TypeId).IsNull();
        await Assert.That((read[2].Quantity, read[2].Unit)).IsEqualTo((0.5m, "l"));
    }

    [Test]
    public async Task Values_that_do_not_fit_are_dropped_and_the_rest_kept()
    {
        var space = await ItemSpace.MakeAsync();
        var json = $$"""
            {"items":[
              {"name":"","typeId":"","quantity":0,"unit":"","fields":[]},
              {"name":"Hammer","typeId":"{{space.Tool.Id}}","quantity":1,"unit":"","fields":[
                {"fieldId":"{{space.Field("power")}}","value":"Steam"},
                {"fieldId":"{{space.Field("price")}}","value":"cheap"},
                {"fieldId":"{{space.Field("bought")}}","value":"last summer"},
                {"fieldId":"{{Guid.NewGuid()}}","value":"x"},
                {"fieldId":"{{space.Field("brand")}}","value":"Estwing"},
                {"fieldId":"{{space.Field("brand")}}","value":"Stanley"}]},
              {"name":"Mystery","typeId":"{{Guid.NewGuid()}}","quantity":-3,"unit":"","fields":[
                {"fieldId":"{{space.Field("brand")}}","value":"Acme"}]}
            ]}
            """;

        var read = TransformationEngine.ReadItemSuggestions(json, Types(space));

        await Assert.That(read.Count).IsEqualTo(2);
        var hammer = read[0];
        await Assert.That(hammer.Quantity).IsNull();
        await Assert.That(hammer.Fields.Keys).IsEquivalentTo([space.Field("brand")]);
        await Assert.That(hammer.Fields[space.Field("brand")].GetString()).IsEqualTo("Estwing");
        await Assert.That(read[1].TypeId).IsNull();
        await Assert.That(read[1].Fields).IsEmpty();
        await Assert.That(read[1].Quantity).IsNull();
    }

    [Test]
    [Arguments("not json at all")]
    [Arguments("[1, 2]")]
    [Arguments("""{"items":"a drill"}""")]
    [Arguments("""{"things":[]}""")]
    public async Task Nonsense_reads_as_no_suggestions(string json)
    {
        var space = await ItemSpace.MakeAsync();

        await Assert.That(TransformationEngine.ReadItemSuggestions(json, Types(space))).IsEmpty();
    }

    [Test]
    public async Task Asking_sends_the_photo_and_the_spaces_types_and_saves_nothing()
    {
        var space = await ItemSpace.MakeAsync();
        await AiTestSetup.UseClaudeAsync(space.Core);
        space.Core.Ai.Answers[ItemsFromPhotoPrompt.SchemaName] = """{"items":[{"name":"Baseball","typeId":"","quantity":0,"unit":"","fields":[]}]}""";
        var commits = space.Core.Messages.Commits;

        var answer = await space.Items.SuggestFromPhotoAsync(space.Owen, space.SpaceId, AiTestSetup.Photo, default);

        await Assert.That(answer.Model).IsEqualTo("Claude");
        await Assert.That(answer.Items.Single().Name).IsEqualTo("Baseball");
        var question = space.Core.Ai.Asked.Single().Question;
        await Assert.That(question.Images.Single().MediaType).IsEqualTo("image/jpeg");
        await Assert.That(question.Text).Contains(space.Tool.Id.ToString());
        await Assert.That(question.JsonSchema).Contains(space.Field("brand").ToString());
        await Assert.That(space.Core.Items.Rows).IsEmpty();
        await Assert.That(space.Core.Messages.Commits).IsEqualTo(commits);
    }

    [Test]
    public async Task Photos_need_an_editor_a_model_and_a_picture()
    {
        var space = await ItemSpace.MakeAsync();

        var noModel = await TestData.Caught<RuleViolationException>(() => space.Items.SuggestFromPhotoAsync(space.Owen, space.SpaceId, AiTestSetup.Photo, default));
        await AiTestSetup.UseClaudeAsync(space.Core);
        var viewer = await TestData.Caught<NotAllowedException>(() => space.Items.SuggestFromPhotoAsync(space.Vera, space.SpaceId, AiTestSetup.Photo, default));
        var notPhoto = await TestData.Caught<RuleViolationException>(() => space.Items.SuggestFromPhotoAsync(space.Owen, space.SpaceId, new([[1, 2, 3]]), default));
        var none = await TestData.Caught<RuleViolationException>(() => space.Items.SuggestFromPhotoAsync(space.Owen, space.SpaceId, new([]), default));

        await Assert.That(noModel!.Message).Contains("AI assistants");
        await Assert.That(viewer).IsNotNull();
        await Assert.That(notPhoto!.Message).Contains("JPEG");
        await Assert.That(none!.Message).Contains("photo");
        await Assert.That(space.Core.Ai.Asked).IsEmpty();
    }

    [Test]
    public async Task A_nameplate_fills_the_items_fields_without_saving()
    {
        var space = await ItemSpace.MakeAsync();
        await AiTestSetup.UseClaudeAsync(space.Core);
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with { TypeId = space.Tool.Id }, default);
        space.Core.Ai.Answers[ItemDetailsFromPhotoPrompt.SchemaName] =
            $$"""{"found":true,"fields":[{"fieldId":"{{space.Field("brand")}}","value":"Makita"},{"fieldId":"{{space.Field("price")}}","value":"n/a"}]}""";

        var answer = await space.Items.SuggestDetailsFromPhotoAsync(space.Owen, space.SpaceId, drill.Id, AiTestSetup.Photo, default);

        await Assert.That(answer.Fields.Keys).IsEquivalentTo([space.Field("brand")]);
        await Assert.That(answer.Fields[space.Field("brand")].GetString()).IsEqualTo("Makita");
        await Assert.That(space.Core.Ai.Asked.Single().Question.Text).Contains("Drill");
        await Assert.That(space.Core.Items.Named("Drill").Fields).IsEmpty();
    }

    [Test]
    public async Task No_label_found_reads_as_no_values_and_an_item_without_fields_is_refused()
    {
        var space = await ItemSpace.MakeAsync();
        await AiTestSetup.UseClaudeAsync(space.Core);
        var drill = await space.Items.CreateItemAsync(space.Owen, space.SpaceId, ItemSpace.Request("Drill") with { TypeId = space.Tool.Id }, default);
        var box = await space.BoxAsync("Box F");
        space.Core.Ai.Answers[ItemDetailsFromPhotoPrompt.SchemaName] =
            $$"""{"found":false,"fields":[{"fieldId":"{{space.Field("brand")}}","value":"Makita"}]}""";

        var answer = await space.Items.SuggestDetailsFromPhotoAsync(space.Owen, space.SpaceId, drill.Id, AiTestSetup.Photo, default);
        var refused = await TestData.Caught<RuleViolationException>(() =>
            space.Items.SuggestDetailsFromPhotoAsync(space.Owen, space.SpaceId, box.Id, AiTestSetup.Photo, default));

        await Assert.That(answer.Fields).IsEmpty();
        await Assert.That(refused!.Message).Contains("no fields");
    }

    private static List<ItemType> Types(ItemSpace space) =>
        space.Core.Types.Rows.Where(type => type.SpaceId == space.SpaceId).ToList();
}
