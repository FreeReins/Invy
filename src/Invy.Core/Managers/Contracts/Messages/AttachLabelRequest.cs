namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Sticks an unclaimed label on an item that already exists.</summary>
/// <param name="ItemId">The item. A label it had before becomes unclaimed again.</param>
public sealed record AttachLabelRequest(Guid ItemId);
