using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>QR labels.</content>
internal static partial class Mapping
{
    public static LabelResponse ToResponse(this Label label, ItemRef? item) =>
        new(label.Code, label.SpaceId, item, label.CreatedAt, label.PrintedAt);
}
