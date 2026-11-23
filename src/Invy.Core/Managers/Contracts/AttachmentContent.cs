namespace Invy.Core.Managers.Contracts;

/// <summary>An attachment's bytes to send, with what to call them. The caller disposes the stream.</summary>
/// <param name="Content">The file, open for reading.</param>
/// <param name="ContentType">Its media type.</param>
/// <param name="Name">The file name to download it as.</param>
public sealed record AttachmentContent(Stream Content, string ContentType, string Name);
