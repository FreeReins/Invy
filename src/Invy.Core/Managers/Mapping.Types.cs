using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Item types and their fields.</content>
internal static partial class Mapping
{
    public static ItemTypeResponse ToResponse(this ItemType type) =>
        new(type.Id, type.Name, type.Icon, type.Colour, type.HoldsItems, type.Fields.Select(field => field.ToResponse()).ToList(), type.Version);

    /// <summary>A stored or starter type as a request, with its field ids cleared so new ones are given.</summary>
    public static ItemTypeRequest ToRequest(this ItemType type) =>
        new(type.Name, type.Icon, type.Colour, type.HoldsItems, type.Fields.Select(field => field.ToResponse() with { Id = null }).ToList(), type.Version);

    public static FieldDefinition ToResponse(this TypeField field) =>
        new(field.Id, field.Key, field.Label, field.Kind, field.Unit, field.Options, field.Required, field.Sortable, field.Remind, field.RepeatMonths);
}
