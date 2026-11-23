using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

/// <summary>Every row of the Search table in docs/developers/design.md, and how terms combine.</summary>
public class QueryTests
{
    private static readonly QueryEngine Engine = new();

    private static readonly DateTimeOffset Now = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly TypeField Brand = Field("brand", FieldKind.Text, sortable: true);
    private static readonly TypeField Price = Field("price", FieldKind.Money, sortable: true);
    private static readonly TypeField Power = Field("power", FieldKind.Select, options: ["Hand", "Cordless"]);
    private static readonly TypeField Bought = Field("bought", FieldKind.Date, sortable: true);
    private static readonly TypeField Isbn = Field("isbn", FieldKind.Barcode);
    private static readonly TypeField Weight = Field("weight", FieldKind.Number);
    private static readonly TypeField Season = Field("season", FieldKind.MultiSelect, options: ["Summer", "Winter"]);

    private static readonly ItemType Tool = new() { Name = "Tool", Fields = [Brand, Price, Power, Bought, Weight] };
    private static readonly ItemType Book = new() { Name = "Book", Fields = [Isbn, Season] };
    private static readonly ItemType Box = new() { Name = "Box", HoldsItems = true };

    private static TypeField Field(string key, FieldKind kind, bool sortable = false, List<string>? options = null) =>
        new() { Id = Guid.NewGuid(), Key = key, Label = char.ToUpperInvariant(key[0]) + key[1..], Kind = kind, Sortable = sortable, Options = options ?? [] };

    private static ItemQuery Build(string text, string? sort = null) => Engine.Build(text, [Tool, Book, Box], Now, sort);

    private static QueryNode Only(string text) => Build(text).Filter;

    /// <summary>The query read from <paramref name="text"/>, written out so it can be compared.</summary>
    private static string Read(string text) => Show(Build(text).Filter);

    private static string Show(QueryNode node) => node switch
    {
        AllOf all => $"all({string.Join(", ", all.Parts.Select(Show))})",
        AnyOf any => $"any({string.Join(", ", any.Parts.Select(Show))})",
        Not not => $"not({Show(not.Part)})",
        Nothing => "nothing",
        Words words => $"words({words.Text})",
        OfType type => $"type({string.Join(", ", type.TypeIds.Select(Name))})",
        Inside inside => $"in({inside.Container})",
        Tagged tag => $"tag({tag.Tag})",
        FieldRange range => $"range({string.Join(", ", range.FieldIds.Select(Name))} {(range.Number is { } n ? Show(n) : Show(range.Date!))})",
        QuantityIn qty => $"qty {Show(qty.Range)}",
        SeenIn seen => $"seen {Show(seen.Range)}{(seen.OrNever ? " or never" : string.Empty)}",
        AddedIn added => $"added {Show(added.Range)}",
        Flag flag => $"{flag.Kind}({string.Join(", ", flag.FieldIds.Select(Name))})",
        _ => node.ToString()!,
    };

    private static string Show<T>(Range<T> range)
        where T : struct
    {
        static string Value(T? value) => value switch
        {
            DateTimeOffset at => at.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            null => string.Empty,
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
        };

        return $"{(range.LowInclusive ? "[" : "(")}{Value(range.Low)}..{Value(range.High)}{(range.HighInclusive ? "]" : ")")}";
    }

    private static string Name(Guid id) =>
        new[] { Tool, Book, Box }.FirstOrDefault(type => type.Id == id)?.Name
        ?? new[] { Brand, Price, Power, Bought, Isbn, Weight, Season }.First(field => field.Id == id).Key;

    // plain words

    [Test]
    public async Task Plain_words_are_each_required_and_rank_the_results()
    {
        var query = Build("cordless drill");

        await Assert.That(Show(query.Filter)).IsEqualTo("all(words(cordless), words(drill))");
        await Assert.That(string.Join(", ", query.Words)).IsEqualTo("cordless, drill");
        await Assert.That(query.Sort).IsEqualTo(SearchOrder.Relevance);
    }

    [Test]
    public async Task A_quoted_phrase_is_one_term()
    {
        await Assert.That(Read("\"box f\"")).IsEqualTo("words(box f)");
    }

    // type:

    [Test]
    public async Task Type_is_found_by_name_ignoring_case()
    {
        await Assert.That(Read("type:tool")).IsEqualTo("type(Tool)");
        var unknown = await TestData.Caught<RuleViolationException>(() => Task.FromResult(Build("type:car")));
        await Assert.That(unknown?.Message).IsEqualTo("There is no type called car. The types are Book, Box, Tool.");
    }

    // in:

    [Test]
    public async Task In_names_a_container_quoted_or_not()
    {
        await Assert.That(Read("in:\"Box F\"")).IsEqualTo("in(Box F)");
        await Assert.That(Read("in:shed")).IsEqualTo("in(shed)");
    }

    // <field>:value

    [Test]
    public async Task A_field_value_is_matched_by_the_field_kind()
    {
        var text = (FieldIs)Only("brand:DeWalt");
        await Assert.That(text.Matches.Single().LowerText).IsEqualTo("dewalt");

        var choice = (FieldIs)Only("power:cordless");
        await Assert.That(choice.Matches.Single().Json?.GetString()).IsEqualTo("Cordless");

        var several = (FieldIs)Only("season:winter");
        await Assert.That(several.Matches.Single().Json?.GetRawText()).IsEqualTo("[\"Winter\"]");

        var number = (FieldIs)Only("price:50");
        await Assert.That(number.Matches.Single().Json?.GetDecimal()).IsEqualTo(50m);
    }

    [Test]
    [Arguments("power:petrol", "Power has no choice called petrol. The choices are Hand, Cordless.")]
    [Arguments("price:cheap", "Price needs a number, such as price:50 or price>50.")]
    [Arguments("colour:red", "No field has the search name colour. Search a field by its search name, shown on the Types tab, such as brand:dewalt.")]
    public async Task A_field_value_that_cannot_match_is_explained(string text, string problem)
    {
        var refused = await TestData.Caught<RuleViolationException>(() => Task.FromResult(Build(text)));

        await Assert.That(refused?.Message).IsEqualTo(problem);
    }

    // <field>>n, <n, ..

    [Test]
    public async Task Ranges_on_sortable_numbers_and_dates()
    {
        await Assert.That(Read("price>50")).IsEqualTo("range(price (50..))");
        await Assert.That(Read("price<=20")).IsEqualTo("range(price (..20])");
        await Assert.That(Read("price:10..20")).IsEqualTo("range(price [10..20])");
        await Assert.That(Read("bought:2025-01-01..2025-12-31")).IsEqualTo("range(bought [01/01/2025..12/31/2025])");
    }

    [Test]
    public async Task A_range_on_a_field_that_is_not_sortable_is_explained()
    {
        var refused = await TestData.Caught<RuleViolationException>(() => Task.FromResult(Build("weight>2")));

        await Assert.That(refused?.Message).StartsWith("Weight can't be compared. Turn on Sort and compare");
    }

    [Test]
    public async Task Qty_compares_the_amount()
    {
        await Assert.That(Read("qty<5")).IsEqualTo("qty (..5)");
        await Assert.That(Read("qty:3")).IsEqualTo("qty [3..3]");
    }

    // seen>1y, added<30d

    [Test]
    public async Task Seen_and_added_take_a_time_ago_or_a_date()
    {
        await Assert.That(Read("seen>1y")).IsEqualTo("seen (..2025-11-23 12:00) or never");
        await Assert.That(Read("seen<2w")).IsEqualTo("seen (2026-11-09 12:00..)");
        await Assert.That(Read("added<30d")).IsEqualTo("added (2026-10-24 12:00..)");
        await Assert.That(Read("added>2026-01-31")).IsEqualTo("added [2026-02-01 00:00..)");
        await Assert.That(Read("added:2026-01-31")).IsEqualTo("added [2026-01-31 00:00..2026-02-01 00:00)");
        await Assert.That(await TestData.Caught<RuleViolationException>(() => Task.FromResult(Build("added:30d")))).IsNotNull();
    }

    // is: / has:

    [Test]
    public async Task Is_and_has_check_what_an_item_is_or_has()
    {
        await Assert.That(Read("is:container")).IsEqualTo("Container()");
        await Assert.That(Read("is:counted")).IsEqualTo("Counted()");
        await Assert.That(Read("is:missing")).IsEqualTo("Missing()");
        await Assert.That(Read("is:lent")).IsEqualTo("Lent()");
        await Assert.That(Read("has:barcode")).IsEqualTo("HasBarcode(isbn)");
        await Assert.That(Read("has:photo")).IsEqualTo("nothing");
        await Assert.That(await TestData.Caught<RuleViolationException>(() => Task.FromResult(Build("is:shiny")))).IsNotNull();
    }

    [Test]
    public async Task Disposed_items_are_left_out_unless_asked_for()
    {
        await Assert.That(Build("drill").IncludeGone).IsFalse();
        await Assert.That(Build("is:disposed").IncludeGone).IsTrue();
        await Assert.That(Build("-is:disposed").IncludeGone).IsFalse();
    }

    [Test]
    public async Task Tags_are_matched_in_lower_case()
    {
        await Assert.That(Read("tag:Garden")).IsEqualTo("tag(garden)");
    }

    // combining

    [Test]
    public async Task Or_makes_alternatives_and_minus_leaves_out()
    {
        await Assert.That(Read("drill type:tool OR saw -is:disposed"))
            .IsEqualTo("any(all(words(drill), type(Tool)), all(words(saw), not(Disposed())))");
    }

    [Test]
    public async Task The_whole_example_from_the_design_reads()
    {
        var query = Build("drill type:tool in:\"Shed\" brand:dewalt price>50 seen>1y qty<5 is:container has:barcode tag:garden");

        await Assert.That(((AllOf)query.Filter).Parts.Count).IsEqualTo(10);
        await Assert.That(string.Join(", ", query.Words)).IsEqualTo("drill");
    }

    [Test]
    public async Task An_empty_search_matches_everything_by_name()
    {
        var query = Build("  ");

        await Assert.That(Show(query.Filter)).IsEqualTo("all()");
        await Assert.That(query.Sort).IsEqualTo(SearchOrder.Name);
        await Assert.That(Build("", "recent").Sort).IsEqualTo(SearchOrder.Recent);
    }
}
