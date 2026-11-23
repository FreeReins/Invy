using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Command: make the small copy of a photo just attached (the images queue). Making it twice changes nothing.</summary>
/// <param name="SpaceId">The space the attachment is in.</param>
/// <param name="AttachmentId">The attachment.</param>
[MessageIdentity("invy.make-thumbnail.v1")]
public sealed record MakeThumbnail(Guid SpaceId, Guid AttachmentId);
