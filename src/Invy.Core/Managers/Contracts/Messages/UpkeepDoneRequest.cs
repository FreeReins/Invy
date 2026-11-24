namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Marks the upkeep in one of an item's repeating date fields as done today.</summary>
/// <param name="FieldId">The date field, which must remind and repeat.</param>
public sealed record UpkeepDoneRequest(Guid FieldId);
