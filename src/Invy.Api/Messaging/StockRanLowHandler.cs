using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace Invy.Api.Messaging;

/// <summary>
/// Sends the low-stock alert to the webhook. Safe to run twice: it checks the item is still low first, and a second
/// alert says the same thing. A webhook that is down is tried again later by the message bus.
/// </summary>
[StickyHandler(MessageQueues.Notifications)]
public static class StockRanLowHandler
{
    public static async Task Handle(StockRanLow message, IMaintenanceManager maintenance, ILogger<StockRanLow> logger, CancellationToken cancellationToken)
    {
        var run = await maintenance.SendLowStockAsync(message, cancellationToken);
        if (run.Problems.Count > 0)
        {
            logger.LogWarning("A low-stock alert wasn't delivered: {Problems}", string.Join("; ", run.Problems));
        }
    }
}
