using System.Security.Cryptography;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="IFileStoreAccess"/>
/// <param name="folder">The files folder (Data:Files, by default Data:Root/files).</param>
internal sealed class FileStoreAccess(string folder) : IFileStoreAccess
{
    public async Task<StoredFile> SaveAsync(Guid spaceId, byte[] data, string extension, CancellationToken cancellationToken)
    {
        var sha = Convert.ToHexStringLower(SHA256.HashData(data));
        var path = $"{spaceId:D}/{sha}.{extension.TrimStart('.')}";
        var full = Full(path);
        if (!File.Exists(full))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);

            // Written beside it, then renamed, so a half-written file never has the final name.
            var partial = $"{full}.{Guid.NewGuid():N}.partial";
            await File.WriteAllBytesAsync(partial, data, cancellationToken);
            File.Move(partial, full, overwrite: true);
        }

        return new StoredFile(path, sha, data.LongLength);
    }

    public async Task WriteAsync(string path, byte[] data, CancellationToken cancellationToken)
    {
        var full = Full(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var partial = $"{full}.{Guid.NewGuid():N}.partial";
        await File.WriteAllBytesAsync(partial, data, cancellationToken);
        File.Move(partial, full, overwrite: true);
    }

    public async Task<byte[]?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var full = Full(path);
        return File.Exists(full) ? await File.ReadAllBytesAsync(full, cancellationToken) : null;
    }

    public Stream? OpenRead(string path)
    {
        var full = Full(path);
        return File.Exists(full) ? new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true) : null;
    }

    public bool Exists(string path) => File.Exists(Full(path));

    public void Delete(string path) => File.Delete(Full(path));

    public void DeleteSpace(Guid spaceId)
    {
        var space = Full($"{spaceId:D}");
        if (Directory.Exists(space))
        {
            Directory.Delete(space, recursive: true);
        }
    }

    /// <summary>The path on disk, refused if it would lead outside the files folder.</summary>
    private string Full(string path)
    {
        var root = Path.GetFullPath(folder);
        var full = Path.GetFullPath(Path.Combine(root, path));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The attachment path {path} leads outside the files folder.");
        }

        return full;
    }
}
