using System.Text.RegularExpressions;

namespace Invy.Core.Utilities;

/// <summary>Search names for fields, made from their labels: "Set number" becomes set_number.</summary>
internal static partial class FieldKeys
{
    /// <summary>The search name for <paramref name="label"/>, or null when no letter is left to start it.</summary>
    public static string? FromLabel(string label)
    {
        var key = NotKeyCharacters().Replace(label.Trim().ToLowerInvariant(), "_").Trim('_');
        key = key.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_');
        if (key.Length > 40)
        {
            key = key[..40].TrimEnd('_');
        }

        return key.Length > 0 && char.IsAsciiLetterLower(key[0]) ? key : null;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NotKeyCharacters();
}
