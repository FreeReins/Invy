using System.Text.Json;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// One field's value to match: <paramref name="Json"/> to be contained in the item's fields (choices, numbers, dates,
/// yes or no), or <paramref name="LowerText"/> to equal the field's text ignoring case.
/// </summary>
internal sealed record FieldMatch(Guid FieldId, JsonElement? Json, string? LowerText);
