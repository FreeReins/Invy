namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>Where a page of items sorted by name then id ended: the next page starts after it.</summary>
internal sealed record ItemCursor(string Name, Guid Id);
