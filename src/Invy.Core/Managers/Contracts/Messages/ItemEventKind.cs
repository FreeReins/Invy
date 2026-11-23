using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What happened to an item, in its timeline.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ItemEventKind>))]
public enum ItemEventKind
{
    /// <summary>Added to Invy, in the place it went to.</summary>
    Created,

    /// <summary>Put in another container, or taken out of one.</summary>
    Moved,

    /// <summary>Its name, type or fields changed. The changes are listed.</summary>
    Changed,

    /// <summary>Its amount went up or down.</summary>
    QuantityChanged,

    /// <summary>Seen where Invy says it is.</summary>
    Seen,

    /// <summary>Lent to someone.</summary>
    Lent,

    /// <summary>Back from being lent.</summary>
    Returned,

    /// <summary>A photo was added.</summary>
    PhotoAdded,

    /// <summary>Marked missing.</summary>
    Missing,

    /// <summary>Found again.</summary>
    Found,

    /// <summary>Thrown away, sold or given away.</summary>
    Disposed,

    /// <summary>Brought back after being disposed.</summary>
    Restored,
}
