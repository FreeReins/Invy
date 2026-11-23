namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item's page: the item, where it is, and how much is inside it.</summary>
/// <param name="Item">The item.</param>
/// <param name="Chain">The containers it is in, top first, such as Shed, Shelf 2, Box ABC.</param>
/// <param name="Contents">How many items are directly inside it, counted now.</param>
public sealed record ItemDetailResponse(ItemResponse Item, IReadOnlyList<ItemRef> Chain, int Contents);
