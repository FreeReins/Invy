namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A type and what its items are worth.</summary>
/// <param name="TypeId">The type, or null for items without one.</param>
/// <param name="Name">The type's name, or "No type".</param>
/// <param name="Count">How many current items have it.</param>
/// <param name="Valued">How many of them have a value.</param>
/// <param name="Value">Their total value.</param>
public sealed record TypeValue(Guid? TypeId, string Name, int Count, int Valued, decimal Value);
