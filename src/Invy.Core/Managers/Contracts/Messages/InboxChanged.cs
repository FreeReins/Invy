using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Event: messages were added to, read in or cleared from one person's inbox, or everyone's.</summary>
/// <param name="UserId">The person whose inbox changed. Null when it was everyone's.</param>
[MessageIdentity("invy.inbox-changed.v1")]
public sealed record InboxChanged(Guid? UserId);
