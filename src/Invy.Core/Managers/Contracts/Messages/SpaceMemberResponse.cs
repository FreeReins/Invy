namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A person in a space.</summary>
/// <param name="UserId">Their account's id.</param>
/// <param name="Username">The name they sign in with.</param>
/// <param name="DisplayName">The name shown in the app.</param>
/// <param name="Role">What they can do in the space.</param>
public sealed record SpaceMemberResponse(Guid UserId, string Username, string DisplayName, SpaceRole Role);
