namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A member's new role in a space.</summary>
/// <param name="Role">What they can do in the space.</param>
public sealed record SpaceMemberRoleRequest(SpaceRole Role);
