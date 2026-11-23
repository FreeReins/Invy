namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>Where a page of events, newest first, ended: the next page holds older ones.</summary>
internal sealed record EventCursor(DateTimeOffset At, Guid Id);
