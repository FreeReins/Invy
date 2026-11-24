using Invy.Core.Engines.Transformation;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>SuggestTypeFields: the fields a model suggests for a type, each one the Types editor would accept.</summary>
public class TypeFieldSuggestionTests
{
    [Test]
    public async Task Suggested_fields_get_search_names_from_their_labels()
    {
        var json = """
            {"fields":[
              {"label":"Set number","kind":"Text","unit":"","options":[],"sortable":true},
              {"label":"Pieces","kind":"Number","unit":"","options":[],"sortable":true},
              {"label":"Theme","kind":"Select","unit":"","options":["City","Technic","city"],"sortable":false},
              {"label":"Price paid","kind":"Money","unit":"USD","options":[],"sortable":true}
            ]}
            """;

        var fields = TransformationEngine.ReadTypeFields(json, []);

        await Assert.That(fields.Select(field => field.Key)).IsEquivalentTo(["set_number", "pieces", "theme", "price_paid"]);
        await Assert.That(fields[2].Options).IsEquivalentTo(["City", "Technic"]);
        await Assert.That(fields[3].Unit).IsEqualTo("USD");
        await Assert.That(fields.All(field => field.Id is null && !field.Required)).IsTrue();
    }

    [Test]
    public async Task Taken_and_unusable_fields_are_dropped_and_odd_ones_tidied()
    {
        var json = """
            {"fields":[
              {"label":"Name","kind":"Text","unit":"","options":[],"sortable":false},
              {"label":"Brand","kind":"Text","unit":"","options":[],"sortable":false},
              {"label":"Colour","kind":"Text","unit":"m","options":["Red"],"sortable":false},
              {"label":"Colour","kind":"Text","unit":"","options":[],"sortable":false},
              {"label":"Size","kind":"Select","unit":"","options":[],"sortable":false},
              {"label":"2024","kind":"Number","unit":"","options":[],"sortable":false},
              {"label":"Owner","kind":"Item","unit":"","options":[],"sortable":false},
              {"label":"Weight","kind":"Wobbly","unit":"","options":[],"sortable":false}
            ]}
            """;

        var fields = TransformationEngine.ReadTypeFields(json, ["name", "brand"]);

        await Assert.That(fields.Select(field => field.Key)).IsEquivalentTo(["colour", "size"]);
        await Assert.That((fields[0].Unit, fields[0].Options.Count)).IsEqualTo(((string?)null, 0));
        await Assert.That(fields[1].Kind).IsEqualTo(FieldKind.Text);
        await Assert.That(TransformationEngine.ReadTypeFields("nonsense", [])).IsEmpty();
    }

    [Test]
    public async Task Asking_leaves_out_what_the_type_has_and_saves_nothing()
    {
        var space = await ItemSpace.MakeAsync();
        await AiTestSetup.UseClaudeAsync(space.Core);
        space.Core.Ai.Answers[SuggestTypeFieldsPrompt.SchemaName] = """
            {"fields":[{"label":"Theme","kind":"Text","unit":"","options":[],"sortable":false},
                       {"label":"Pieces","kind":"Number","unit":"","options":[],"sortable":true},
                       {"label":"Tag","kind":"Text","unit":"","options":[],"sortable":false}]}
            """;
        var types = space.Core.Types.Rows.Count;

        var answer = await space.Core.TypeManager().SuggestFieldsAsync(space.Owen, space.SpaceId, new("Lego sets", ["Theme"]), default);

        await Assert.That(answer.Fields.Select(field => field.Key)).IsEquivalentTo(["pieces"]);
        await Assert.That(answer.Model).IsEqualTo("Claude");
        await Assert.That(space.Core.Ai.Asked.Single().Question.Text).Contains("Lego sets");
        await Assert.That(space.Core.Types.Rows.Count).IsEqualTo(types);
    }

    [Test]
    public async Task A_type_needs_a_name_and_an_editor()
    {
        var space = await ItemSpace.MakeAsync();
        await AiTestSetup.UseClaudeAsync(space.Core);
        var types = space.Core.TypeManager();

        var noName = await TestData.Caught<RuleViolationException>(() => types.SuggestFieldsAsync(space.Owen, space.SpaceId, new("  "), default));
        var viewer = await TestData.Caught<NotAllowedException>(() => types.SuggestFieldsAsync(space.Vera, space.SpaceId, new("Lego sets"), default));

        await Assert.That(noName!.Message).Contains("Name the type");
        await Assert.That(viewer).IsNotNull();
        await Assert.That(space.Core.Ai.Asked).IsEmpty();
    }
}
