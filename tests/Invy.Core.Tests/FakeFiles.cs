using System.Security.Cryptography;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>The files folder kept in memory, by path.</summary>
internal sealed class FakeFiles : IFileStoreAccess
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public Task<StoredFile> SaveAsync(Guid spaceId, byte[] data, string extension, CancellationToken cancellationToken)
    {
        var sha = Convert.ToHexStringLower(SHA256.HashData(data));
        var path = $"{spaceId:D}/{sha}.{extension}";
        Files.TryAdd(path, data);
        return Task.FromResult(new StoredFile(path, sha, data.LongLength));
    }

    public Task WriteAsync(string path, byte[] data, CancellationToken cancellationToken)
    {
        Files[path] = data;
        return Task.CompletedTask;
    }

    public Task<byte[]?> ReadAsync(string path, CancellationToken cancellationToken) => Task.FromResult(Files.GetValueOrDefault(path));

    public Stream? OpenRead(string path) => Files.TryGetValue(path, out var data) ? new MemoryStream(data, writable: false) : null;

    public bool Exists(string path) => Files.ContainsKey(path);

    public void Delete(string path) => Files.Remove(path);

    public void DeleteSpace(Guid spaceId)
    {
        foreach (var path in Files.Keys.Where(path => path.StartsWith($"{spaceId:D}/", StringComparison.Ordinal)).ToList())
        {
            Files.Remove(path);
        }
    }
}
