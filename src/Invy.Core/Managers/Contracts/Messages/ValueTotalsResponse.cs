namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What a space's things are worth, in total, by place at the top and by type. Currencies aren't converted: a total is a plain sum.</summary>
/// <param name="Total">The value of every current item.</param>
/// <param name="Items">How many current items there are.</param>
/// <param name="Unvalued">How many of them have no value (no money field, or it is empty).</param>
/// <param name="ByPlace">Each container at the top with what is inside it, most valuable first; a Place of null is the things at the top outside any container.</param>
/// <param name="ByType">Each type with its items, most valuable first; a TypeId of null is the items without a type.</param>
public sealed record ValueTotalsResponse(decimal Total, int Items, int Unvalued, IReadOnlyList<PlaceValue> ByPlace, IReadOnlyList<TypeValue> ByType);
