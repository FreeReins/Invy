using System.Security.Cryptography;
using Invy.Core.Managers.Contracts;

namespace Invy.Core.Engines;

/// <summary>Makes and reads QR label codes: 4 to 6 characters from an alphabet without look-alikes.</summary>
internal sealed class LabelEngine
{
    /// <summary>Digits and capitals, leaving out those easily misread: 0 O, 1 I L, 5 S, 8 B, 2 Z.</summary>
    public const string Alphabet = "34679ACDEFGHJKMNPQRTUVWXY";

    public const int MinLength = 4;

    public const int MaxLength = 6;

    /// <summary>The most labels made in one batch.</summary>
    public const int BatchLimit = 500;

    /// <summary>
    /// How long new codes are, from how many labels the install has: 4 characters until about 5% of them are taken,
    /// then 5, then 6, so a random new code rarely clashes.
    /// </summary>
    public int LengthFor(int existing) => existing switch
    {
        < 20_000 => 4,
        < 500_000 => 5,
        _ => 6,
    };

    /// <summary>Up to <paramref name="count"/> distinct random codes of <paramref name="length"/> characters.</summary>
    public IReadOnlyList<string> NewCodes(int count, int length) =>
        Enumerable.Range(0, count).Select(_ => RandomNumberGenerator.GetString(Alphabet, length)).Distinct().ToList();

    /// <summary>
    /// The code in what was scanned or typed: the code itself in either case (7k3q), or an address ending in /l/7K3Q.
    /// </summary>
    /// <exception cref="RuleViolationException">It isn't a label code.</exception>
    public string Code(string? text)
    {
        // A route value keeps %2F encoded, so a scanned address passed whole arrives half decoded.
        var code = Uri.UnescapeDataString((text ?? string.Empty).Trim());
        var cut = code.IndexOfAny(['?', '#']);
        code = (cut >= 0 ? code[..cut] : code).TrimEnd('/');
        var slash = code.LastIndexOf('/');
        code = code[(slash + 1)..].ToUpperInvariant();
        return code.Length is >= MinLength and <= MaxLength && code.All(Alphabet.Contains)
            ? code
            : throw new RuleViolationException($"\"{text?.Trim()}\" isn't an Invy label. Label codes are 4 to 6 letters and digits, such as 7K3Q.");
    }

    /// <summary>The name of an item made by claiming a label without giving one, such as Box 7K3Q.</summary>
    public string DefaultName(string code) => $"Box {code}";
}
