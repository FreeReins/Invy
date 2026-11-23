namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item's timeline and where it has been.</summary>
/// <param name="Events">Its own events and what went in and out of it, newest first.</param>
/// <param name="Places">Where it has been, newest first, counting its containers' moves as well as its own.</param>
public sealed record ItemHistoryResponse(IReadOnlyList<ItemEventResponse> Events, IReadOnlyList<ItemStay> Places);
