namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One custom field of an item type. Items store values by the field's id, so renaming it changes no items.</summary>
/// <param name="Id">The field's stable id. Null for a new field; Invy gives it one.</param>
/// <param name="Key">The name used in search, such as <c>brand</c> in <c>brand:dewalt</c>.</param>
/// <param name="Label">The name shown, such as Brand.</param>
/// <param name="Kind">What kind of value it holds.</param>
/// <param name="Unit">For numbers and money: the unit or currency shown after the value, such as kg or USD.</param>
/// <param name="Options">For select and multiselect fields: the choices, in order. Empty for other kinds.</param>
/// <param name="Required">Shown as needed when filling in. It never stops an item being saved.</param>
/// <param name="Sortable">Also kept as a typed value, for sorting and ranges such as <c>price&gt;50</c>.</param>
public sealed record FieldDefinition(
    Guid? Id, string Key, string Label, FieldKind Kind, string? Unit, IReadOnlyList<string> Options, bool Required, bool Sortable);
