namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Someone with an account on this install, to add to a space.</summary>
/// <param name="Username">The name they sign in with.</param>
/// <param name="Role">What they can do in the space.</param>
public sealed record AddSpaceMemberRequest(string Username, SpaceRole Role);
