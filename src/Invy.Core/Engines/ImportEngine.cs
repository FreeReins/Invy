using System.Globalization;
using System.Text;
using Invy.Core.Engines.Contracts.Messages;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>
/// Reads CSV files for imports (RFC 4180, comma, semicolon or tab separated), recognises Homebox's export, suggests
/// where each column goes, and reads each row through a mapping. Reads and saves nothing.
/// </summary>
internal sealed class ImportEngine
{
    /// <summary>The largest file an import takes.</summary>
    public const int MaxBytes = 20 * 1024 * 1024;

    /// <summary>The most rows one import reads.</summary>
    public const int MaxRows = 100_000;

    /// <summary>The simple targets a column can have, besides <c>field:&lt;id&gt;</c>.</summary>
    public static readonly IReadOnlyList<string> Targets =
        ["name", "marking", "quantity", "unit", "tags", "notes", "note", "location", "type", "ignore"];

    private static readonly char[] LocationSeparators = ['/', '>', '›', '\\'];

    private static readonly char[] TagSeparators = [',', ';', '|'];

    /// <summary>Where Homebox's columns go. Columns not named here (HB.field.*, purchase details) become notes.</summary>
    private static readonly Dictionary<string, string> HomeboxTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HB.name"] = "name",
        ["HB.quantity"] = "quantity",
        ["HB.description"] = "notes",
        ["HB.notes"] = "notes",
        ["HB.location"] = "location",
        ["HB.labels"] = "tags",
        ["HB.asset_id"] = "marking",
        ["HB.import_ref"] = "ignore",
        ["HB.archived"] = "ignore",
        ["HB.insured"] = "ignore",
        ["HB.lifetime_warranty"] = "ignore",
    };

    /// <summary>Plain headings people use, and where they go.</summary>
    private static readonly Dictionary<string, string> CommonTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = "name", ["item"] = "name", ["title"] = "name",
        ["marking"] = "marking", ["asset"] = "marking", ["asset id"] = "marking", ["asset tag"] = "marking",
        ["quantity"] = "quantity", ["qty"] = "quantity", ["amount"] = "quantity", ["count"] = "quantity",
        ["unit"] = "unit", ["units"] = "unit",
        ["tags"] = "tags", ["tag"] = "tags", ["labels"] = "tags", ["categories"] = "tags", ["category"] = "tags",
        ["notes"] = "notes", ["note"] = "notes", ["description"] = "notes", ["comments"] = "notes",
        ["location"] = "location", ["place"] = "location", ["where"] = "location", ["room"] = "location", ["container"] = "location",
        ["type"] = "type", ["kind"] = "type",
    };

    /// <summary>The file's rows, heading first. Blank lines are skipped.</summary>
    /// <exception cref="RuleViolationException">The file is empty, too big, or has no heading.</exception>
    public List<string[]> Parse(byte[] data)
    {
        if (data.Length == 0)
        {
            throw new RuleViolationException("That file is empty. Export a CSV with a heading row, then try again.");
        }

        if (data.Length > MaxBytes)
        {
            throw new RuleViolationException($"An import can be up to {MaxBytes / 1024 / 1024} MB. Split the file and import each part.");
        }

        var text = new UTF8Encoding(false).GetString(data).TrimStart('﻿');
        var separator = Separator(text);
        var rows = new List<string[]>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (c == separator)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                EndRow();
            }
            else
            {
                cell.Append(c);
            }
        }

        EndRow();
        if (rows.Count == 0 || rows[0].All(heading => heading.Trim().Length == 0))
        {
            throw new RuleViolationException("That file has no heading row. Its first line should name the columns, such as Name,Quantity,Location.");
        }

        if (rows.Count - 1 > MaxRows)
        {
            throw new RuleViolationException($"An import reads up to {MaxRows:N0} rows. Split the file and import each part.");
        }

        return rows;

        void EndRow()
        {
            row.Add(cell.ToString());
            cell.Clear();
            if (row.Any(value => value.Trim().Length > 0))
            {
                rows.Add([.. row]);
            }

            row.Clear();
        }
    }

    /// <summary>True for Homebox's export, whose columns start with HB.</summary>
    public static ImportKind KindOf(IReadOnlyList<string> headers) =>
        headers.Count(heading => heading.Trim().StartsWith("HB.", StringComparison.OrdinalIgnoreCase)) >= 2 ? ImportKind.Homebox : ImportKind.Csv;

    /// <summary>Where each column probably goes, by its heading and the fields of <paramref name="type"/>.</summary>
    public IReadOnlyList<ColumnMapping> Suggest(IReadOnlyList<string> headers, ImportKind kind, ItemType? type)
    {
        var used = new HashSet<string>();
        var mapping = new List<ColumnMapping>();
        for (var column = 0; column < headers.Count; column++)
        {
            var heading = headers[column].Trim();
            var target = kind == ImportKind.Homebox
                ? HomeboxTargets.GetValueOrDefault(heading) ?? "note"
                : CommonTargets.GetValueOrDefault(heading)
                  ?? type?.Fields.Where(field => string.Equals(field.Key, heading, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(field.Label, heading, StringComparison.OrdinalIgnoreCase))
                      .Select(field => $"field:{field.Id}").FirstOrDefault()
                  ?? "note";

            // Only the first column can be the name, amount or type; later ones become notes.
            if (target is "name" or "quantity" or "unit" or "type" or "marking" or "location" && !used.Add(target))
            {
                target = "note";
            }

            mapping.Add(new ColumnMapping(column, target));
        }

        return mapping;
    }

    /// <summary>Checks a mapping: known targets, real columns, the import type's fields, and one name column.</summary>
    public static IReadOnlyList<ColumnMapping> Check(IReadOnlyList<ColumnMapping> mapping, int columns, ItemType? type)
    {
        var checkedMapping = new List<ColumnMapping>();
        foreach (var entry in mapping.Where(entry => entry.Target != "ignore"))
        {
            if (entry.Column < 0 || entry.Column >= columns)
            {
                throw new RuleViolationException($"The file has columns 0 to {columns - 1}, not {entry.Column}.");
            }

            var target = entry.Target.Trim();
            if (target.StartsWith("field:", StringComparison.Ordinal))
            {
                if (!Guid.TryParse(target["field:".Length..], out var fieldId) || type?.Fields.Any(field => field.Id == fieldId) != true)
                {
                    throw new RuleViolationException("A column is mapped to a field the import's type doesn't have. Pick the type first, then its fields.");
                }
            }
            else if (!Targets.Contains(target))
            {
                throw new RuleViolationException($"A column can go to {string.Join(", ", Targets)}, or a field of the import's type.");
            }

            checkedMapping.Add(entry with { Target = target });
        }

        var names = checkedMapping.Count(entry => entry.Target == "name");
        return names == 1
            ? checkedMapping
            : throw new RuleViolationException("Pick exactly one column for the name.");
    }

    /// <summary>One row read through a checked mapping.</summary>
    /// <exception cref="RuleViolationException">The row can't be added; the message says why.</exception>
    public ImportRow Read(string[] row, IReadOnlyList<string> headers, IReadOnlyList<ColumnMapping> mapping, ImportKind kind)
    {
        string Cell(int column) => column < row.Length ? row[column].Trim() : string.Empty;

        string? name = null, marking = null, unit = null, typeName = null;
        decimal? quantity = null;
        var tags = new List<string>();
        var notes = new List<string>();
        var location = new List<string>();
        var fields = new Dictionary<Guid, string>();
        foreach (var entry in mapping)
        {
            var value = Cell(entry.Column);
            if (value.Length == 0)
            {
                continue;
            }

            switch (entry.Target)
            {
                case "name":
                    name = value;
                    break;
                case "marking":
                    marking = value;
                    break;
                case "unit":
                    unit = value;
                    break;
                case "type":
                    typeName = value;
                    break;
                case "quantity":
                    quantity = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount >= 0
                        ? amount
                        : throw new RuleViolationException($"\"{value}\" isn't an amount.");
                    break;
                case "tags":
                    tags.AddRange(value.Split(TagSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.ToLowerInvariant()));
                    break;
                case "notes":
                    notes.Add(value);
                    break;
                case "note":
                    notes.Add($"{Heading(headers, entry.Column)}: {value}");
                    break;
                case "location":
                    location.AddRange(value.Split(LocationSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                    break;
                default:
                    fields[Guid.Parse(entry.Target["field:".Length..])] = value;
                    break;
            }
        }

        if (name is null)
        {
            throw new RuleViolationException("It has no name.");
        }

        // Homebox gives everything an amount, 1 for a single thing.
        if (kind == ImportKind.Homebox && quantity == 1)
        {
            quantity = null;
        }

        if (unit is not null && quantity is null)
        {
            unit = null;
        }

        return new ImportRow(name, marking, quantity, unit, tags.Distinct().ToList(), notes.Count == 0 ? null : string.Join("\n\n", notes),
            location, typeName, fields);
    }

    /// <summary>A column's heading without Homebox's prefixes, such as Serial number for HB.serial_number.</summary>
    public static string Heading(IReadOnlyList<string> headers, int column)
    {
        var heading = column < headers.Count ? headers[column].Trim() : $"Column {column + 1}";
        foreach (var prefix in new[] { "HB.field.", "HB." })
        {
            if (heading.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var plain = heading[prefix.Length..].Replace('_', ' ');
                return plain.Length == 0 ? heading : char.ToUpperInvariant(plain[0]) + plain[1..];
            }
        }

        return heading;
    }

    /// <summary>The separator the heading row uses most: comma, semicolon or tab.</summary>
    private static char Separator(string text)
    {
        var end = text.IndexOfAny(['\r', '\n']);
        var heading = end < 0 ? text : text[..end];
        return new[] { ',', ';', '\t' }.OrderByDescending(separator => heading.Count(c => c == separator)).First();
    }
}
