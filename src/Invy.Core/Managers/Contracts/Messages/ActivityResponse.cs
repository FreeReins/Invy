namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A page of a space's timeline, newest first.</summary>
/// <param name="Events">The events.</param>
/// <param name="Next">Pass as <c>before</c> for older events; null when there are no more.</param>
public sealed record ActivityResponse(IReadOnlyList<ItemEventResponse> Events, string? Next);
