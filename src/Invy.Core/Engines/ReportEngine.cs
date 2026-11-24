using System.Globalization;
using System.Text;
using System.Text.Json;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>
/// Writes reports as CSV: one header and one row per item, with its room, place, value, dates, field values and photo
/// address. Every text cell that a spreadsheet would read as a formula is written as text instead.
/// </summary>
internal sealed class ReportEngine
{
    private static readonly string[] Fixed =
        ["Room", "Place", "Name", "Marking", "Type", "Amount", "Unit", "Value", "Status", "Label", "Last seen", "Added", "Tags", "Photo"];

    /// <summary>The field columns: one per search name across the space's types, labelled by the first field with it.</summary>
    public IReadOnlyList<TypeField> FieldColumns(IReadOnlyList<ItemType> types) =>
        types.SelectMany(type => type.Fields).GroupBy(field => field.Key).Select(group => group.First()).ToList();

    /// <summary>The header line.</summary>
    public string Header(IReadOnlyList<TypeField> fields) => Line(Fixed.Concat(fields.Select(field => field.Label)));

    /// <summary>One item's line. <paramref name="photoAddress"/> turns a photo id into the address it is downloaded from.</summary>
    public string Row(SearchResult result, IReadOnlyList<ItemType> types, IReadOnlyList<TypeField> fields, Func<Guid, string> photoAddress)
    {
        var item = result.Item;
        var details = result.Details;
        var type = types.FirstOrDefault(candidate => candidate.Id == item.TypeId);
        var cells = new List<string?>
        {
            result.Chain.Count > 0 ? result.Chain[0].Name : null,
            string.Join(" › ", result.Chain.Select(place => place.Name)),
            item.Name,
            item.Marking,
            type?.Name,
            Number(item.Quantity),
            item.Unit,
            Number(item.Value),
            item.Status.ToString(),
            item.LabelCode,
            Date(details?.LastSeenAt),
            Date(details?.CreatedAt),
            details is null ? null : string.Join("; ", details.Tags),
            details?.PhotoId is Guid photo ? photoAddress(photo) : null,
        };

        foreach (var column in fields)
        {
            // Types can share a search name, each with its own field id: the item's own type's one counts.
            var field = type?.Fields.FirstOrDefault(candidate => candidate.Key == column.Key);
            cells.Add(field is not null && details is not null && details.Fields.TryGetValue(field.Id, out var value) ? Text(value) : null);
        }

        return Line(cells);
    }

    /// <summary>A CSV line ending in CRLF, each cell quoted when it needs to be.</summary>
    public string Line(IEnumerable<string?> cells) => string.Join(',', cells.Select(Cell)) + "\r\n";

    private static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // A cell starting like a formula (=, +, -, @, tab, return) runs in a spreadsheet: a quote first keeps it text.
        // Numbers this engine wrote itself are left alone, so a negative amount still reads as a number.
        if ("=+-@\t\r".Contains(value[0]) && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    private static string? Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static string? Date(DateTimeOffset? value) => value?.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? Text(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Array => string.Join("; ", value.EnumerateArray().Select(Text)),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        _ => value.GetRawText(),
    };

    /// <summary>A file name safe on every system: "Home insurance inventory 2026-11-24.csv".</summary>
    public string FileName(string spaceName, string report, DateTimeOffset at)
    {
        var safe = new StringBuilder();
        foreach (var c in $"{spaceName} {report} {at.UtcDateTime:yyyy-MM-dd}")
        {
            safe.Append(char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '-');
        }

        return safe.ToString().Trim() + ".csv";
    }
}
