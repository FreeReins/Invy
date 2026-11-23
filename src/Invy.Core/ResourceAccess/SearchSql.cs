using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Npgsql;
using NpgsqlTypes;

namespace Invy.Core.ResourceAccess;

/// <summary>
/// Turns an <see cref="ItemQuery"/> into the WHERE clause and rank of a query on <c>"Items" i</c>. Every value is a
/// parameter; the SQL text holds only column names and operators chosen here. Make a new one for each command, since
/// a parameter belongs to one command.
/// </summary>
internal sealed partial class SearchSql
{
    private readonly List<NpgsqlParameter> parameters = [];

    private readonly string space;

    public SearchSql(Guid spaceId)
    {
        space = Add(spaceId);
    }

    public IReadOnlyList<NpgsqlParameter> Parameters => parameters;

    /// <summary>The conditions on <c>i</c>: in the space, the query's filter, and not gone unless asked for.</summary>
    public string Where(ItemQuery query)
    {
        var gone = query.IncludeGone ? string.Empty : " AND i.\"Status\" NOT IN ('Disposed', 'Archived')";
        return $"i.\"SpaceId\" = {space}{gone} AND {Node(query.Filter)}";
    }

    /// <summary>How well an item matches the query's words (higher is better), as real; 0 without words.</summary>
    public string Rank(ItemQuery query)
    {
        var tokens = query.Words.SelectMany(Tokens).ToList();
        if (tokens.Count == 0)
        {
            return "0::real";
        }

        var any = Add(string.Join(" | ", tokens.Select(token => $"{token}:*")));
        var words = Add(string.Join(' ', query.Words));
        return $"(ts_rank(i.\"Search\", to_tsquery('simple', {any})) + word_similarity({words}, i.\"Name\"))::real";
    }

    /// <summary>Adds a parameter and returns its name for the SQL.</summary>
    public string Add(object value, NpgsqlDbType? type = null)
    {
        var name = $"p{parameters.Count}";
        parameters.Add(type is NpgsqlDbType dbType ? new NpgsqlParameter(name, dbType) { Value = value } : new NpgsqlParameter(name, value));
        return "@" + name;
    }

    private string Node(QueryNode node) => node switch
    {
        AllOf all => all.Parts.Count == 0 ? "TRUE" : $"({string.Join(" AND ", all.Parts.Select(Node))})",
        AnyOf any => any.Parts.Count == 0 ? "FALSE" : $"({string.Join(" OR ", any.Parts.Select(Node))})",
        Not not => $"(NOT coalesce({Node(not.Part)}, FALSE))",
        Nothing => "FALSE",
        Words words => WordsSql(words.Text),
        OfType type => $"i.\"TypeId\" = ANY({Add(type.TypeIds.ToArray())})",
        Inside inside => InsideSql(inside.Container),
        Contains contains => $"i.\"{contains.Column}\" ILIKE {Add(Like(contains.Text))}",
        Tagged tag => $"i.\"Tags\" @> {Add(new[] { tag.Tag }, NpgsqlDbType.Array | NpgsqlDbType.Text)}",
        FieldIs field => $"({string.Join(" OR ", field.Matches.Select(MatchSql))})",
        FieldRange range => RangeSql(range),
        QuantityIn quantity => $"(i.\"Quantity\" IS NOT NULL AND {Bounds("i.\"Quantity\"", quantity.Range)})",
        SeenIn seen => seen.OrNever
            ? $"(i.\"LastSeenAt\" IS NULL OR {Bounds("i.\"LastSeenAt\"", Utc(seen.Range))})"
            : $"(i.\"LastSeenAt\" IS NOT NULL AND {Bounds("i.\"LastSeenAt\"", Utc(seen.Range))})",
        AddedIn added => Bounds("i.\"CreatedAt\"", Utc(added.Range)),
        Flag flag => FlagSql(flag),
        _ => throw new ArgumentOutOfRangeException(nameof(node), node, "QueryEngine made a condition SearchSql doesn't know."),
    };

    /// <summary>A word or phrase: its words as prefixes in the search text, or the text inside the name or marking, or a near spelling of the name.</summary>
    private string WordsSql(string text)
    {
        var like = Add(Like(text));
        var parts = new List<string> { $"i.\"Name\" ILIKE {like}", $"i.\"Marking\" ILIKE {like}", $"{Add(text)} <% i.\"Name\"" };
        var tokens = Tokens(text).ToList();
        if (tokens.Count > 0)
        {
            // Words in a phrase follow each other; the last may be half typed.
            parts.Insert(0, $"i.\"Search\" @@ to_tsquery('simple', {Add(string.Join(" <-> ", tokens) + ":*")})");
        }

        return $"({string.Join(" OR ", parts)})";
    }

    private string InsideSql(string container)
    {
        var name = Add(container.ToLowerInvariant());
        return $"""
            EXISTS (SELECT 1 FROM "Items" c WHERE c."SpaceId" = {space} AND c."HoldsItems"
                AND (lower(c."Name") = {name} OR lower(c."Marking") = {name} OR lower(c."LabelCode") = {name})
                AND c."Path" @> i."Path" AND c."Id" <> i."Id")
            """;
    }

    private string MatchSql(FieldMatch match)
    {
        if (match.Json is JsonElement json)
        {
            var contained = new StringBuilder("{").Append(JsonSerializer.Serialize(match.FieldId.ToString(), SearchJson.Default.String))
                .Append(':').Append(json.GetRawText()).Append('}').ToString();
            return $"i.\"Fields\" @> {Add(contained, NpgsqlDbType.Jsonb)}";
        }

        return $"lower(i.\"Fields\" ->> {Add(match.FieldId.ToString())}) = {Add(match.LowerText!)}";
    }

    private string RangeSql(FieldRange range)
    {
        var condition = range.Number is { } number ? Bounds("v.\"Num\"", number) : Bounds("v.\"Date\"", range.Date!);
        return $"EXISTS (SELECT 1 FROM \"ItemValues\" v WHERE v.\"ItemId\" = i.\"Id\" AND v.\"FieldId\" = ANY({Add(range.FieldIds.ToArray())}) AND {condition})";
    }

    private string FlagSql(Flag flag) => flag.Kind switch
    {
        ItemFlag.Container => "i.\"HoldsItems\"",
        ItemFlag.Counted => "i.\"Quantity\" IS NOT NULL",
        ItemFlag.Unique => "i.\"Quantity\" IS NULL",
        ItemFlag.HasBarcode => flag.FieldIds.Count == 0
            ? "FALSE"
            : $"i.\"Fields\" ?| {Add(flag.FieldIds.Select(id => id.ToString()).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text)}",
        _ => $"i.\"Status\" = '{flag.Kind}'",
    };

    private string Bounds<T>(string column, Range<T> range)
        where T : struct
    {
        var parts = new List<string>();
        if (range.Low is T low)
        {
            parts.Add($"{column} {(range.LowInclusive ? ">=" : ">")} {Add(low)}");
        }

        if (range.High is T high)
        {
            parts.Add($"{column} {(range.HighInclusive ? "<=" : "<")} {Add(high)}");
        }

        return parts.Count == 0 ? "TRUE" : $"({string.Join(" AND ", parts)})";
    }

    /// <summary>PostgreSQL takes only UTC times for timestamptz parameters.</summary>
    private static Range<DateTimeOffset> Utc(Range<DateTimeOffset> range) =>
        range with { Low = range.Low?.ToUniversalTime(), High = range.High?.ToUniversalTime() };

    /// <summary>A pattern for ILIKE that finds the text anywhere, with its own % and _ taken literally.</summary>
    private static string Like(string text) =>
        "%" + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";

    /// <summary>The letters-and-digits words of the text, in lower case, safe to put in a tsquery.</summary>
    private static IEnumerable<string> Tokens(string text) =>
        WordPattern().Matches(text).Select(match => match.Value.ToLower(CultureInfo.InvariantCulture));

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
