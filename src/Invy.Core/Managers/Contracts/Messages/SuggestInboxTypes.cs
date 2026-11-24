using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// Command (the ai queue): ask an AI model for a type for the space's Inbox items, a batch at a time, keeping each
/// answer as a suggestion. Items already asked about are skipped, so running it twice asks nothing new.
/// </summary>
/// <param name="SpaceId">The space whose Inbox to look at.</param>
/// <param name="AskedAgainAt">
/// When someone asked for suggestions again: items asked about before then without a suggestion are asked about too.
/// Null to ask only about items never asked about.
/// </param>
[MessageIdentity("invy.suggest-inbox-types.v1")]
public sealed record SuggestInboxTypes(Guid SpaceId, DateTimeOffset? AskedAgainAt = null);
