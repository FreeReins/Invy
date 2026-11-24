using System.Globalization;
using System.Text;
using System.Text.Json;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines.Transformation;

/// <summary>
/// Field values in a model's answer: a list of <c>{ fieldId, value }</c> with every value as text, since a schema
/// can't give each field its own kind. Each is turned into the value its field kind stores, or dropped.
/// </summary>
internal static class FieldValues
{
    private const int TextLength = 2_000;

    private const int LongTextLength = 10_000;

    /// <summary>The schema of the list, with field ids limited to <paramref name="fieldIds"/>.</summary>
    public static string Schema(IEnumerable<Guid> fieldIds) => $$"""
        {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "fieldId": { "type": "string", "enum": {{TypeCatalog.IdEnum(fieldIds.Distinct())}} },
              "value": { "type": "string" }
            },
            "required": ["fieldId", "value"],
            "additionalProperties": false
          }
        }
        """;

    /// <summary>
    /// The values in <paramref name="answer"/>'s <paramref name="name"/> list for <paramref name="fields"/>, by field
    /// id. Unknown fields, values that don't fit their kind and repeats after the first are dropped.
    /// </summary>
    public static Dictionary<Guid, JsonElement> Read(JsonElement answer, string name, IEnumerable<TypeField> fields)
    {
        var byId = fields.Where(field => field.Kind != FieldKind.Item).ToDictionary(field => field.Id);
        var values = new Dictionary<Guid, JsonElement>();
        if (!answer.TryGetProperty(name, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return values;
        }

        foreach (var entry in list.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object))
        {
            if (Guid.TryParse(AiAnswer.Text(entry, "fieldId"), out var id)
                && byId.TryGetValue(id, out var field)
                && !values.ContainsKey(id)
                && Value(field, ValueText(entry)) is { } value)
            {
                values[id] = value;
            }
        }

        return values;
    }

    /// <summary>The value <paramref name="text"/> stands for in <paramref name="field"/>'s kind, or null when it doesn't fit.</summary>
    public static JsonElement? Value(TypeField field, string? text)
    {
        if (text is null)
        {
            return null;
        }

        return field.Kind switch
        {
            FieldKind.Number or FieldKind.Money => Number(text) is { } number ? Json(number) : null,
            FieldKind.Bool => Flag(text) is { } flag ? Json(flag) : null,
            FieldKind.Date => Date(text) is { } date ? Json(date) : null,
            FieldKind.Select => Option(field, text) is { } option ? Json(option) : null,
            FieldKind.MultiSelect => Options(field, text) is { Count: > 0 } options ? Json(options) : null,
            FieldKind.Url => Uri.TryCreate(text, UriKind.Absolute, out var url) && url.Scheme is "http" or "https" && text.Length <= TextLength ? Json(text) : null,
            FieldKind.Barcode => Digits(text) is { } digits ? Json(digits) : null,
            FieldKind.LongText => text.Length <= LongTextLength ? Json(text) : null,
            FieldKind.Item => null,
            _ => text.Length <= TextLength ? Json(text) : null,
        };
    }

    /// <summary>A value given as text, or as a number or true/false by a model that ignored the schema.</summary>
    private static string? ValueText(JsonElement entry) =>
        entry.TryGetProperty("value", out var value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False
            ? value.GetRawText()
            : AiAnswer.Text(entry, "value");

    /// <summary>A number such as 12.99, $1,299.00 or 30 cm: everything but digits, the point and a leading minus is dropped.</summary>
    private static decimal? Number(string text)
    {
        var kept = new StringBuilder();
        foreach (var character in text)
        {
            if (char.IsAsciiDigit(character) || character == '.' || (character == '-' && kept.Length == 0))
            {
                kept.Append(character);
            }
            else if (character != ',' && kept.Length > 0 && !char.IsWhiteSpace(character))
            {
                break;
            }
        }

        return decimal.TryParse(kept.ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    private static bool? Flag(string text) => text.Trim().ToLowerInvariant() switch
    {
        "true" or "yes" or "y" => true,
        "false" or "no" or "n" => false,
        _ => null,
    };

    private static string? Date(string text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
        || DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;

    /// <summary>The field's own spelling of the option, matched ignoring case.</summary>
    private static string? Option(TypeField field, string text) =>
        field.Options.FirstOrDefault(option => string.Equals(option, text.Trim(), StringComparison.OrdinalIgnoreCase));

    private static List<string> Options(TypeField field, string text) =>
        text.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(part => Option(field, part))
            .OfType<string>()
            .Distinct()
            .ToList();

    /// <summary>A barcode's digits, with spaces and dashes taken out; null unless 6 to 14 digits are left.</summary>
    private static string? Digits(string text)
    {
        var digits = text.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return digits.Length is >= 6 and <= 14 && digits.All(char.IsAsciiDigit) ? digits : null;
    }

    private static JsonElement Json(decimal value) => JsonSerializer.SerializeToElement(value, AiAnswerJson.Default.Decimal);

    private static JsonElement Json(bool value) => JsonSerializer.SerializeToElement(value, AiAnswerJson.Default.Boolean);

    private static JsonElement Json(string value) => JsonSerializer.SerializeToElement(value, AiAnswerJson.Default.String);

    private static JsonElement Json(List<string> value) => JsonSerializer.SerializeToElement(value, AiAnswerJson.Default.ListString);
}
