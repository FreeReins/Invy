using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>How a view shows the items it finds.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ViewDisplay>))]
public enum ViewDisplay
{
    /// <summary>Rows with the chosen columns, sortable.</summary>
    Table,

    /// <summary>Cards, photos first.</summary>
    Cards,

    /// <summary>The results under their containers.</summary>
    Tree,

    /// <summary>Columns grouped by a select field, such as condition or status.</summary>
    Board,

    /// <summary>The events of the matching items, newest first.</summary>
    Timeline,
}
