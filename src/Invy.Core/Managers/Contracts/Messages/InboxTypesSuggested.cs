using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Event: an AI model suggested types for some of a space's Inbox items, so open Inbox pages load again.</summary>
/// <param name="SpaceId">The space whose Inbox changed.</param>
[MessageIdentity("invy.inbox-types-suggested.v1")]
public sealed record InboxTypesSuggested(Guid SpaceId);
