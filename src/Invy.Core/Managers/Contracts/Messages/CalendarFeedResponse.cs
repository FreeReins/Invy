namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A space's calendar feed: the address to subscribe to in a calendar app.</summary>
/// <param name="Url">The feed's address, with its secret, or null while the feed is off.</param>
public sealed record CalendarFeedResponse(string? Url);
