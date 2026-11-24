namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Where one column of an import's file goes.</summary>
/// <param name="Column">The column, counted from 0.</param>
/// <param name="Target">
/// name, marking, quantity, unit, tags, notes, location (containers, such as Garage / Shelf 2), type (a type's name),
/// note (added to the notes as "Column: value"), field:&lt;field id&gt; (a field of the import's type), or ignore.
/// </param>
public sealed record ColumnMapping(int Column, string Target);
