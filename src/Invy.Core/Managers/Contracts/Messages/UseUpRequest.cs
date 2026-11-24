namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Uses up some or all of what a project set aside of an item.</summary>
/// <param name="ItemId">The item.</param>
/// <param name="Amount">How much was used, up to what is set aside, or null for all of it.</param>
public sealed record UseUpRequest(Guid ItemId, decimal? Amount);
