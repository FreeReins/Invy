namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>One item's value of one date field, from its sortable values.</summary>
internal sealed record ItemDate(Item Item, Guid FieldId, DateOnly On);
