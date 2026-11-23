using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What sort of file is attached to an item.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AttachmentKind>))]
public enum AttachmentKind
{
    /// <summary>A JPEG, PNG, WebP or GIF picture, shown with a thumbnail.</summary>
    Photo,

    /// <summary>A PDF, such as a manual or a receipt.</summary>
    Document,
}
