using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>One person's membership of a space.</summary>
internal sealed class SpaceMember
{
    /// <summary>The account.</summary>
    public Guid UserId { get; set; }

    public SpaceRole Role { get; set; }
}
