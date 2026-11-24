namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Photos for an AI model to look at.</summary>
/// <param name="Photos">One to four JPEG, PNG, WebP or GIF images, base64 in JSON, each up to 10 MB.</param>
public sealed record PhotoRequest(IReadOnlyList<byte[]>? Photos);
