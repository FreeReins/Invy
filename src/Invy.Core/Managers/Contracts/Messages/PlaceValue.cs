namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A place at the top and what is inside it.</summary>
/// <param name="Place">The container at the top, or null for the things at the top outside any container.</param>
/// <param name="Count">How many items it holds, at any depth.</param>
/// <param name="Value">Their total value, the container's own included.</param>
public sealed record PlaceValue(ItemRef? Place, int Count, decimal Value);
