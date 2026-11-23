using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Where an item stands. Disposed and archived items are left out of search unless asked for.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ItemStatus>))]
public enum ItemStatus
{
    /// <summary>Here, where Invy says it is.</summary>
    Active,

    /// <summary>Not where it should be.</summary>
    Missing,

    /// <summary>Lent to someone.</summary>
    Lent,

    /// <summary>Thrown away, sold or given away. It can be restored.</summary>
    Disposed,

    /// <summary>Kept for the record but no longer in use.</summary>
    Archived,
}
