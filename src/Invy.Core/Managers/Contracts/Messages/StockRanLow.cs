using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// Event: a change took a counted item from above its low-stock level to at or below it. The notifications queue
/// sends one alert to the webhook, after checking it is still low and its space still has the Stock feature on.
/// </summary>
/// <param name="SpaceId">The item's space.</param>
/// <param name="ItemId">The item that ran low.</param>
[MessageIdentity("invy.stock-ran-low.v1")]
public sealed record StockRanLow(Guid SpaceId, Guid ItemId);
