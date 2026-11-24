using System.ComponentModel;
using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Api.Mcp;

/// <summary>A field for define_type to give a new type.</summary>
/// <param name="Label">The field's name as people see it, such as Set number.</param>
/// <param name="Kind">What it holds.</param>
/// <param name="Unit">For Number and Money: the unit or currency, such as cm or USD.</param>
/// <param name="Options">For Select and MultiSelect: the choices.</param>
/// <param name="Sortable">True to sort by it and search ranges, such as price&gt;50.</param>
public sealed record NewField(
    [property: Description("The field's name as people see it, such as \"Set number\". Its search name is made from it (set_number).")] string Label,
    [property: Description("What it holds: Text, LongText, Number, Money, Date, Bool, Select, MultiSelect, Url, Barcode, Person or Item.")] FieldKind Kind,
    [property: Description("For Number and Money only: the unit or currency, such as \"cm\" or \"USD\".")] string? Unit = null,
    [property: Description("For Select and MultiSelect only: the choices.")] IReadOnlyList<string>? Options = null,
    [property: Description("True to sort by it and search ranges such as price>50.")] bool Sortable = false);
