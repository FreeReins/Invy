using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>
/// Reads what people type in the search box into an <see cref="ItemQuery"/> (docs/developers/design.md, Search):
/// <c>drill type:tool in:"Shed" brand:dewalt price&gt;50 seen&gt;1y qty&lt;5 is:container has:barcode tag:garden</c>.
/// Terms are all required; <c>OR</c> between them makes alternatives, and <c>-</c> in front leaves matches out. Names
/// of types and fields become ids here, from the space's types, so nothing typed ever reaches SQL as text.
/// </summary>
internal sealed partial class QueryEngine
{
    public const int MaxLength = 500;

    public const int MaxTerms = 30;

    private static readonly string[] Flags = ["container", "counted", "unique", "active", "missing", "lent", "disposed", "archived"];

    /// <summary>Reads <paramref name="text"/> against the space's <paramref name="types"/>, as of <paramref name="now"/>.</summary>
    /// <exception cref="RuleViolationException">A term can't be read; the message says which and how to write it.</exception>
    public ItemQuery Build(string? text, IReadOnlyList<ItemType> types, DateTimeOffset now, string? sort = null)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length > MaxLength)
        {
            throw new RuleViolationException($"A search can have up to {MaxLength} characters.");
        }

        var terms = Tokens(text).ToList();
        if (terms.Count(term => !term.IsOr) > MaxTerms)
        {
            throw new RuleViolationException($"A search can have up to {MaxTerms} terms.");
        }

        var state = new BuildState(types, now);
        var groups = new List<List<QueryNode>> { new() };
        foreach (var term in terms)
        {
            if (term.IsOr)
            {
                groups.Add([]);
                continue;
            }

            var node = state.Read(term);
            groups[^1].Add(term.Negated ? new Not(node) : node);
        }

        var alternatives = groups.Where(group => group.Count > 0).Select(group => group.Count == 1 ? group[0] : new AllOf(group)).ToList();
        QueryNode filter = alternatives.Count switch
        {
            0 => new AllOf([]),
            1 => alternatives[0],
            _ => new AnyOf(alternatives),
        };

        return new ItemQuery(filter, state.IncludeGone, state.Words, Order(sort, state.Words.Count > 0));
    }

    private static SearchOrder Order(string? sort, bool hasWords) => sort?.ToLowerInvariant() switch
    {
        null or "" => hasWords ? SearchOrder.Relevance : SearchOrder.Name,
        "relevance" => hasWords ? SearchOrder.Relevance : SearchOrder.Name,
        "name" => SearchOrder.Name,
        "recent" => SearchOrder.Recent,
        _ => throw new RuleViolationException("Sort by relevance, name or recent."),
    };

    /// <summary>Splits the text into terms at spaces outside quotes.</summary>
    private static IEnumerable<Term> Tokens(string text)
    {
        var current = new StringBuilder();
        var quoted = false;
        foreach (var c in text + " ")
        {
            if (c == '"')
            {
                quoted = !quoted;
                current.Append(c);
            }
            else if (char.IsWhiteSpace(c) && !quoted)
            {
                if (current.Length > 0)
                {
                    yield return Term.Read(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }
    }

    [GeneratedRegex(@"^(?<neg>-)?(?:(?<key>[A-Za-z][A-Za-z0-9_]*)(?<op>:>=|:<=|:>|:<|>=|<=|:|>|<)(?=.))?(?<value>.*)$", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TermPattern();

    [GeneratedRegex(@"^(?<amount>\d+)(?<unit>[dwmy])$", RegexOptions.CultureInvariant)]
    private static partial Regex DurationPattern();

    /// <summary>One term: <c>-key:value</c>, <c>key&gt;value</c>, a word, a "phrase", or OR.</summary>
    private sealed record Term(bool Negated, string? Key, string Op, string Value, bool IsOr)
    {
        public static Term Read(string raw)
        {
            if (raw == "OR")
            {
                return new Term(false, null, string.Empty, string.Empty, true);
            }

            var match = TermPattern().Match(raw);
            var value = match.Groups["value"].Value;
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                value = value[1..^1];
            }
            else
            {
                value = value.Trim('"');
            }

            var key = match.Groups["key"].Success ? match.Groups["key"].Value.ToLowerInvariant() : null;
            return new Term(match.Groups["neg"].Success && raw.Length > 1, key, match.Groups["op"].Value.TrimStart(':') is { Length: > 0 } op ? op : (key is null ? string.Empty : ":"), value.Trim(), false);
        }

        public bool IsRange => Op is ">" or ">=" or "<" or "<=" || (Op == ":" && Value.Contains("..", StringComparison.Ordinal));

        /// <summary>How the term was typed, for messages.</summary>
        public string Typed => $"{Key}{(Op == ":" ? ":" : Op)}{Value}";
    }

    /// <summary>What reading the terms has found so far.</summary>
    private sealed partial class BuildState(IReadOnlyList<ItemType> types, DateTimeOffset now)
    {
        public bool IncludeGone { get; private set; }

        public List<string> Words { get; } = [];


        public QueryNode Read(Term term) => term.Key switch
        {
            null => ReadWords(term),
            "type" => ReadType(term),
            "in" => new Inside(Required(term)),
            "name" => new Contains(ItemText.Name, Required(term)),
            "marking" => new Contains(ItemText.Marking, Required(term)),
            "notes" => new Contains(ItemText.Notes, Required(term)),
            "tag" => new Tagged(Required(term).ToLowerInvariant()),
            "is" or "status" => ReadFlag(term),
            "has" => ReadHas(term),
            "qty" => new QuantityIn(NumberRange(term, "qty>5 or qty:2..10")),
            "seen" => ReadSeen(term),
            "added" => new AddedIn(TimeRange(term, "added<30d or added>2026-01-01")),
            "lent" => new LentTo(Required(term)),
            "going" => new GoingTo(Required(term)),
            "due" => ReadDue(term),
            "project" => new ReservedFor(Required(term)),
            "code" => throw new RuleViolationException("Searching label codes comes with labels, in a later version of Invy."),
            _ => ReadField(term),
        };

        private QueryNode ReadWords(Term term)
        {
            if (term.Value.Length == 0)
            {
                return new AllOf([]);
            }

            if (!term.Negated)
            {
                Words.Add(term.Value);
            }

            return new Words(term.Value);
        }

        private QueryNode ReadType(Term term)
        {
            var name = Required(term);
            var ids = types.Where(type => string.Equals(type.Name, name, StringComparison.CurrentCultureIgnoreCase)).Select(type => type.Id).ToList();
            return ids.Count > 0
                ? new OfType(ids)
                : throw new RuleViolationException(
                    $"There is no type called {name}. {(types.Count == 0 ? "This space has no types yet." : $"The types are {string.Join(", ", types.Select(type => type.Name).Order())}.")}");
        }

        private QueryNode ReadFlag(Term term)
        {
            var value = Required(term).ToLowerInvariant();
            switch (value)
            {
                case "low":
                    return new Flag(ItemFlag.Low, []);
                case "overdue":
                    return new LoanDue(new Range<DateOnly>(null, false, Today, false));
            }

            var index = Array.IndexOf(Flags, value);
            if (index < 0)
            {
                throw new RuleViolationException($"Search with is:{string.Join(", is:", Flags)}, is:low or is:overdue.");
            }

            var flag = (ItemFlag)index;
            if (!term.Negated && flag is ItemFlag.Disposed or ItemFlag.Archived)
            {
                IncludeGone = true;
            }

            return new Flag(flag, []);
        }

        private QueryNode ReadHas(Term term) => Required(term).ToLowerInvariant() switch
        {
            "barcode" => new Flag(ItemFlag.HasBarcode, Fields().Where(field => field.Kind == FieldKind.Barcode).Select(field => field.Id).ToList()),

            // Photos and labels arrive with phase 2; until then nothing has one.
            "photo" or "label" => new Nothing(),
            _ => throw new RuleViolationException("Search with has:photo, has:barcode or has:label."),
        };

        private QueryNode ReadSeen(Term term)
        {
            var range = TimeRange(term, "seen>1y or seen<30d");

            // "Not seen in a year" includes things never seen.
            return new SeenIn(range, range.Low is null);
        }

        private QueryNode ReadField(Term term)
        {
            var fields = Fields().Where(field => field.Key == term.Key).ToList();
            if (fields.Count == 0)
            {
                throw new RuleViolationException(
                    $"No field has the search name {term.Key}. Search a field by its search name, shown on the Types tab, such as brand:dewalt.");
            }

            var value = Required(term);
            if (term.IsRange)
            {
                var dates = fields.Where(field => field.Sortable && field.Kind == FieldKind.Date).Select(field => field.Id).ToList();
                var numbers = fields.Where(field => field.Sortable && field.Kind is FieldKind.Number or FieldKind.Money).Select(field => field.Id).ToList();
                if (dates.Count == 0 && numbers.Count == 0)
                {
                    throw new RuleViolationException(
                        $"{fields[0].Label} can't be compared. Turn on Sort and compare for it on the Types tab, or search for an exact value with {term.Key}:value.");
                }

                var isDate = value.Replace("..", string.Empty, StringComparison.Ordinal).Length > 0
                    && value.Split("..").Where(part => part.Length > 0).All(part => ParseDate(part) is not null);
                return isDate && dates.Count > 0
                    ? new FieldRange(dates, null, DateRange(term))
                    : numbers.Count > 0
                        ? new FieldRange(numbers, NumberRange(term, $"{term.Key}>50 or {term.Key}:10..20"), null)
                        : throw new RuleViolationException($"{fields[0].Label} is a date. Write it like {term.Key}>2026-01-31.");
            }

            var matches = fields.Select(field => Match(field, value)).OfType<FieldMatch>().ToList();
            return matches.Count > 0
                ? new FieldIs(matches)
                : throw new RuleViolationException(fields[0].Kind switch
                {
                    FieldKind.Select or FieldKind.MultiSelect => $"{fields[0].Label} has no choice called {value}. The choices are {string.Join(", ", fields[0].Options)}.",
                    FieldKind.Number or FieldKind.Money => $"{fields[0].Label} needs a number, such as {term.Key}:50 or {term.Key}>50.",
                    FieldKind.Date => $"{fields[0].Label} needs a date, such as {term.Key}:2026-01-31.",
                    FieldKind.Bool => $"{fields[0].Label} is yes or no, such as {term.Key}:yes.",
                    _ => $"{fields[0].Label} can't be searched by value.",
                });
        }

        private static FieldMatch? Match(TypeField field, string value)
        {
            switch (field.Kind)
            {
                case FieldKind.Select:
                case FieldKind.MultiSelect:
                    var option = field.Options.FirstOrDefault(choice => string.Equals(choice, value, StringComparison.CurrentCultureIgnoreCase));
                    if (option is null)
                    {
                        return null;
                    }

                    var json = field.Kind == FieldKind.Select ? JsonSerializer.SerializeToElement(option, QueryJson.Default.String)
                        : JsonSerializer.SerializeToElement(new[] { option }, QueryJson.Default.StringArray);
                    return new FieldMatch(field.Id, json, null);
                case FieldKind.Number or FieldKind.Money:
                    return ParseNumber(value) is decimal number ? new FieldMatch(field.Id, JsonSerializer.SerializeToElement(number, QueryJson.Default.Decimal), null) : null;
                case FieldKind.Date:
                    return ParseDate(value) is DateOnly date
                        ? new FieldMatch(field.Id, JsonSerializer.SerializeToElement(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), QueryJson.Default.String), null)
                        : null;
                case FieldKind.Bool:
                    bool? yes = value.ToLowerInvariant() switch { "yes" or "true" => true, "no" or "false" => false, _ => null };
                    return yes is bool flag ? new FieldMatch(field.Id, JsonSerializer.SerializeToElement(flag, QueryJson.Default.Boolean), null) : null;
                case FieldKind.Item:
                    return null;
                default:
                    return new FieldMatch(field.Id, null, value.ToLowerInvariant());
            }
        }

        private IEnumerable<TypeField> Fields() => types.SelectMany(type => type.Fields);

        private static string Required(Term term) =>
            term.Value.Length > 0 ? term.Value : throw new RuleViolationException($"{term.Key}: needs something after it, such as {term.Key}:box.");

        private static Range<decimal> NumberRange(Term term, string example) =>
            Range(term, part => ParseNumber(part), $"{term.Key} needs a number, such as {example}.");

        private static Range<DateOnly> DateRange(Term term) =>
            Range(term, part => ParseDate(part), $"{term.Key} needs a date, such as {term.Key}>2026-01-31.");

        /// <summary>
        /// A time range from a duration (<c>seen&gt;1y</c>: longer ago than a year; <c>added&lt;30d</c>: within 30 days) or
        /// from days (<c>added&gt;2026-01-31</c>: after that day; <c>added:2026-01-31</c>: on it).
        /// </summary>
        private Range<DateTimeOffset> TimeRange(Term term, string example)
        {
            var problem = $"{term.Key} needs a time, such as {example}. Times are days (d), weeks (w), months (m) or years (y) ago, or a date.";
            if (DurationPattern().Match(term.Value) is { Success: true } duration)
            {
                var amount = int.Parse(duration.Groups["amount"].Value, CultureInfo.InvariantCulture);
                var since = duration.Groups["unit"].Value switch
                {
                    "d" => now.AddDays(-amount),
                    "w" => now.AddDays(-7 * amount),
                    "m" => now.AddMonths(-amount),
                    _ => now.AddYears(-Math.Min(amount, 1000)),
                };
                return term.Op switch
                {
                    ">" or ">=" => new Range<DateTimeOffset>(null, false, since, term.Op == ">="),
                    "<" or "<=" => new Range<DateTimeOffset>(since, term.Op == "<=", null, false),
                    _ => throw new RuleViolationException($"Use > or < with a time ago: {term.Key}>{term.Value} for longer ago, {term.Key}<{term.Value} for more recently."),
                };
            }

            var days = Range(term, part => ParseDate(part), problem);
            DateTimeOffset? Start(DateOnly? day, bool next) =>
                day is DateOnly d ? new DateTimeOffset(d.AddDays(next ? 1 : 0).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

            // Whole days: "after the 31st" starts on the 1st, "up to the 31st" runs to its end.
            return new Range<DateTimeOffset>(
                Start(days.Low, !days.LowInclusive), true,
                Start(days.High, days.HighInclusive), false);
        }

        /// <summary>A range from <c>&gt;a</c>, <c>&gt;=a</c>, <c>&lt;b</c>, <c>&lt;=b</c>, <c>:a..b</c> (both included) or <c>:a</c> (just a).</summary>
        private static Range<T> Range<T>(Term term, Func<string, T?> parse, string problem)
            where T : struct
        {
            T Read(string part) => parse(part) ?? throw new RuleViolationException(problem);

            switch (term.Op)
            {
                case ">": return new Range<T>(Read(term.Value), false, null, false);
                case ">=": return new Range<T>(Read(term.Value), true, null, false);
                case "<": return new Range<T>(null, false, Read(term.Value), false);
                case "<=": return new Range<T>(null, false, Read(term.Value), true);
            }

            var parts = term.Value.Split("..");
            if (parts.Length == 2)
            {
                return new Range<T>(parts[0].Length > 0 ? Read(parts[0]) : null, true, parts[1].Length > 0 ? Read(parts[1]) : null, true);
            }

            var only = Read(term.Value);
            return new Range<T>(only, true, only, true);
        }

        private static decimal? ParseNumber(string text) =>
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

        private static DateOnly? ParseDate(string text) =>
            DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
    }
}
