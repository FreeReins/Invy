namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>What one container at the top holds, from its rollup, or (with no id) the things at the top outside any container.</summary>
/// <param name="PlaceId">The container at the top, or null for things at the top that hold nothing.</param>
/// <param name="Count">How many current items it holds at any depth, or how many things are at the top.</param>
/// <param name="Value">Their total value, the container's own included. Currencies aren't converted.</param>
internal sealed record PlaceTotal(Guid? PlaceId, int Count, decimal Value);
