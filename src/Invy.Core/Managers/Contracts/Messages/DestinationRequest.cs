namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Sets or clears where a box goes in a move.</summary>
/// <param name="DestinationId">A place or container in the same space, or null to clear it.</param>
public sealed record DestinationRequest(Guid? DestinationId);
