using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>
/// Attached files on disk, under Data:Root/files/&lt;space id&gt;/. A file is named by its SHA-256, so it is written
/// once and never changed. Paths are always the ones this accessor handed out.
/// </summary>
internal interface IFileStoreAccess
{
    /// <summary>Writes the bytes into the space's folder, unless the same file is already there.</summary>
    Task<StoredFile> SaveAsync(Guid spaceId, byte[] data, string extension, CancellationToken cancellationToken);

    /// <summary>Writes a file Invy made from another, such as a thumbnail, at <paramref name="path"/>.</summary>
    Task WriteAsync(string path, byte[] data, CancellationToken cancellationToken);

    /// <summary>The file's bytes, or null when it isn't there.</summary>
    Task<byte[]?> ReadAsync(string path, CancellationToken cancellationToken);

    /// <summary>Opens the file for sending, or null when it isn't there.</summary>
    Stream? OpenRead(string path);

    bool Exists(string path);

    /// <summary>Removes the file if it is there.</summary>
    void Delete(string path);

    /// <summary>Removes a space's whole folder, after the space is deleted.</summary>
    void DeleteSpace(Guid spaceId);
}
