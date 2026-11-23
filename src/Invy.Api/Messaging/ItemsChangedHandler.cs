using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace Invy.Api.Messaging;

/// <summary>Recounts the contents and value totals of the containers whose items changed. Recounting twice is harmless.</summary>
[StickyHandler(MessageQueues.Rollups)]
public static class ItemsChangedHandler
{
    public static Task Handle(ItemsChanged message, IItemManager items, CancellationToken cancellationToken) =>
        items.UpdateRollupsAsync(message, cancellationToken);
}
