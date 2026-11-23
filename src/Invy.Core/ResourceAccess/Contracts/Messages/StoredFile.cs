namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A file just written under the files folder.</summary>
/// <param name="Path">Where it is, relative to the files folder.</param>
/// <param name="Sha256">Its SHA-256, lower-case hex, which also names it.</param>
/// <param name="Bytes">Its size.</param>
internal sealed record StoredFile(string Path, string Sha256, long Bytes);
