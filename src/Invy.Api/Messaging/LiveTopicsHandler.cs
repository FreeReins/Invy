using Invy.Api.Live;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Wolverine.Attributes;

namespace Invy.Api.Messaging;

/// <summary>
/// Tells the open pages which part of Invy each event changed, so they load it again. It only nudges: the managers
/// already saved the change, and the pages read it through the API.
/// </summary>
[StickyHandler(MessageQueues.Live)]
public static class LiveTopicsHandler
{
    public static void Handle(InboxChanged message, LiveUpdates live) => live.Broadcast(LiveTopics.Inbox);

    public static void Handle(InboxTypesSuggested message, LiveUpdates live) => live.Broadcast(LiveTopics.InboxItems);
}
