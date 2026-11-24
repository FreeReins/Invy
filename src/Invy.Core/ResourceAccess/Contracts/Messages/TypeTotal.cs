namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>The current items of one type (or of none) and their value.</summary>
/// <param name="TypeId">The type, or null for items without one.</param>
/// <param name="Count">How many current items have it.</param>
/// <param name="Valued">How many of them have a value.</param>
/// <param name="Value">Their total value. Currencies aren't converted.</param>
internal sealed record TypeTotal(Guid? TypeId, int Count, int Valued, decimal Value);
