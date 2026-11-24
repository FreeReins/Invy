using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Views.</content>
internal static partial class Mapping
{
    public static ViewResponse ToResponse(this View view, Guid userId, SpaceRole role) => new(
        view.Id, view.Name, view.Query, view.Display, [.. view.Columns], view.Sort, view.GroupBy, view.Shared, view.Pinned,
        view.OwnerId == userId, ValidationEngine.CanChangeView(view, userId, role), view.UpdatedAt);
}
