using System.Globalization;
using System.Text.RegularExpressions;

namespace Invy.Core.Engines;

/// <summary>
/// Reads a quick add line such as "baseball, glove, 2 bats, 3.5 kg filament, screws x200" into items. A name with an
/// amount becomes a counted item; one without (or with an amount of 1 and no unit) is unique.
/// </summary>
internal sealed partial class QuickAddEngine
{
    /// <summary>The most items one quick add makes.</summary>
    public const int MaxItems = 100;

    private static readonly HashSet<string> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        "kg", "g", "mg", "lb", "lbs", "oz", "t", "l", "ml", "cl", "dl", "m", "cm", "mm", "km", "ft", "in", "yd", "gal", "pt", "qt",
    };

    /// <summary>The items in <paramref name="text"/>, in the order typed. Blank parts are skipped.</summary>
    public IReadOnlyList<QuickItem> Parse(string? text) =>
        Separators().Split(text ?? string.Empty)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .Select(Item)
            .ToList();

    private static QuickItem Item(string part)
    {
        // "2 bats", "3.5 kg filament", "2x bats"
        var leading = Leading().Match(part);
        if (leading.Success && leading.Groups["name"].Value.Trim().Length > 0)
        {
            var name = leading.Groups["name"].Value.Trim();
            var unit = leading.Groups["unit"].Success ? leading.Groups["unit"].Value : null;

            // "2 kg" alone isn't an item; "3 g" with nothing after reads as the name.
            if (unit is not null && !Units.Contains(unit))
            {
                name = $"{unit} {name}";
                unit = null;
            }

            return Counted(Capitalise(name), Number(leading.Groups["amount"].Value), unit);
        }

        // "screws x200", "screws × 200"
        var trailing = Trailing().Match(part);
        if (trailing.Success && trailing.Groups["name"].Value.Trim().Length > 0)
        {
            return Counted(Capitalise(trailing.Groups["name"].Value.Trim()), Number(trailing.Groups["amount"].Value), null);
        }

        return new QuickItem(Capitalise(part), null, null);
    }

    private static QuickItem Counted(string name, decimal amount, string? unit) =>
        amount == 1 && unit is null ? new QuickItem(name, null, null) : new QuickItem(name, amount, unit?.ToLowerInvariant());

    private static decimal Number(string text) => decimal.Parse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);

    private static string Capitalise(string name) => char.ToUpper(name[0], CultureInfo.CurrentCulture) + name[1..];

    [GeneratedRegex(@"[,;\n\r]+")]
    private static partial Regex Separators();

    [GeneratedRegex(@"^(?<amount>\d+(?:[.,]\d+)?)\s*(?:x|×)?\s+(?:(?<unit>[a-zA-Z]{1,3})\s+)?(?<name>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Leading();

    [GeneratedRegex(@"^(?<name>.+?)\s+(?:x|×)\s*(?<amount>\d+(?:[.,]\d+)?)$", RegexOptions.CultureInvariant)]
    private static partial Regex Trailing();

    /// <summary>One item read from the text: its name, and its amount and unit when counted.</summary>
    internal sealed record QuickItem(string Name, decimal? Quantity, string? Unit);
}
