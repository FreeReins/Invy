namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A photo or PDF to attach to an item.</summary>
/// <param name="Name">The file's name, such as receipt.pdf, shown in the list and used when it is downloaded.</param>
/// <param name="Data">The file's bytes, base64 in JSON. JPEG, PNG, WebP, GIF or PDF, up to 10 MB.</param>
public sealed record AddAttachmentRequest(string Name, byte[] Data);
