using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace Invy.Api.Messaging;

/// <summary>Makes the thumbnail of a photo just attached. Making it twice changes nothing.</summary>
[StickyHandler(MessageQueues.Images)]
public static class MakeThumbnailHandler
{
    public static Task Handle(MakeThumbnail command, IAttachmentManager attachments, CancellationToken cancellationToken) =>
        attachments.MakeThumbnailAsync(command, cancellationToken);
}
