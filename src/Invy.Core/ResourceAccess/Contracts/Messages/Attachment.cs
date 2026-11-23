using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A file attached to an item. The bytes are under Data:Root/files, at <see cref="Path"/>: named by their SHA-256, so
/// the same photo attached twice is stored once and a stored file never changes.
/// </summary>
internal sealed class Attachment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SpaceId { get; set; }

    public Guid ItemId { get; set; }

    public AttachmentKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    /// <summary>Where the file is under the files folder: "&lt;space id&gt;/&lt;sha256&gt;.&lt;extension&gt;".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The file's SHA-256, lower-case hex.</summary>
    public string Sha256 { get; set; } = string.Empty;

    public long Bytes { get; set; }

    public bool HasThumbnail { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Where a file's thumbnail is kept: next to it, as a JPEG.</summary>
    public static string ThumbnailPath(string path) => System.IO.Path.ChangeExtension(path, ".thumb.jpg");
}
