using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace Invy.Api.Messaging;

/// <summary>Asks an AI model for types for a batch of a space's Inbox items. Items already asked about are skipped, so running it twice asks nothing new.</summary>
[StickyHandler(MessageQueues.Ai)]
public static class SuggestInboxTypesHandler
{
    public static Task Handle(SuggestInboxTypes command, IItemManager items, CancellationToken cancellationToken) =>
        items.SweepInboxTypesAsync(command, cancellationToken);
}
