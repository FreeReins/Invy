using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>
/// Spaces and their members. Each call checks your role in the space: any member can look, owners rename and delete
/// the space and manage who is in it. Being an install admin gives no access to a space.
/// </summary>
public static class SpaceEndpoints
{
    /// <summary>The spaces you are in, oldest first, with your role in each.</summary>
    [WolverineGet("/api/spaces")]
    public static Task<IReadOnlyList<SpaceResponse>> List(
        ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.ListSpacesAsync(Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>The starter packs of item types a new space can begin with.</summary>
    [WolverineGet("/api/starter-packs")]
    public static IReadOnlyList<StarterPackResponse> StarterPacks([FromServices] ISpaceManager spaces) => spaces.ListStarterPacks();

    /// <summary>The features a space can switch on and off, in the order their switches are shown.</summary>
    [WolverineGet("/api/space-features")]
    public static IReadOnlyList<SpaceFeatureResponse> Features([FromServices] ISpaceManager spaces) => spaces.ListFeatures();

    /// <summary>
    /// Makes a space with you as its owner, optionally from a starter pack, with its features, places and examples.
    /// </summary>
    [WolverinePost("/api/spaces")]
    public static Task<SpaceResponse> Create(
        CreateSpaceRequest request, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.CreateSpaceAsync(Sessions.RequireUserId(principal), request, cancellationToken);

    /// <summary>Renames a space. Owners only.</summary>
    [WolverinePut("/api/spaces/{spaceId}")]
    public static Task<SpaceResponse> Rename(
        Guid spaceId, RenameSpaceRequest request, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces,
        CancellationToken cancellationToken) =>
        spaces.RenameSpaceAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Deletes a space and everything in it. Owners only.</summary>
    [WolverineDelete("/api/spaces/{spaceId}")]
    public static Task Delete(Guid spaceId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.DeleteSpaceAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>The people in a space, owners first.</summary>
    [WolverineGet("/api/spaces/{spaceId}/members")]
    public static Task<IReadOnlyList<SpaceMemberResponse>> Members(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.ListMembersAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Adds someone with an account on this install by username. Owners only. Returns the members.</summary>
    [WolverinePost("/api/spaces/{spaceId}/members")]
    public static Task<IReadOnlyList<SpaceMemberResponse>> AddMember(
        Guid spaceId, AddSpaceMemberRequest request, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces,
        CancellationToken cancellationToken) =>
        spaces.AddMemberAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Changes a member's role. Owners only; a space keeps at least one owner. Returns the members.</summary>
    [WolverinePut("/api/spaces/{spaceId}/members/{memberId}")]
    public static Task<IReadOnlyList<SpaceMemberResponse>> SetRole(
        Guid spaceId, Guid memberId, SpaceMemberRoleRequest request, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces,
        CancellationToken cancellationToken) =>
        spaces.SetMemberRoleAsync(Sessions.RequireUserId(principal), spaceId, memberId, request, cancellationToken);

    /// <summary>Removes someone from a space (owners), or leaves it (anyone). Returns the members.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/members/{memberId}")]
    public static Task<IReadOnlyList<SpaceMemberResponse>> RemoveMember(
        Guid spaceId, Guid memberId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.RemoveMemberAsync(Sessions.RequireUserId(principal), spaceId, memberId, cancellationToken);

    /// <summary>Switches a space's features on and off: the ones sent are on, the rest off. Owners only.</summary>
    [WolverinePut("/api/spaces/{spaceId}/features")]
    public static Task<SpaceResponse> SetFeatures(
        Guid spaceId, SpaceFeaturesRequest request, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces,
        CancellationToken cancellationToken) =>
        spaces.SetFeaturesAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Changes a space's kind: its hints and getting-started steps, never its types. Owners only.</summary>
    [WolverinePut("/api/spaces/{spaceId}/kind")]
    public static Task<SpaceResponse> SetKind(
        Guid spaceId, SpaceKindRequest request, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces,
        CancellationToken cancellationToken) =>
        spaces.SetKindAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Removes the space's example items. One still holding your own things is kept. Editors and owners.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/examples")]
    public static Task<RemoveExamplesResponse> RemoveExamples(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.RemoveExamplesAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Your getting-started card for a space: its kind's steps, each ticked from what the space has.</summary>
    [WolverineGet("/api/spaces/{spaceId}/getting-started")]
    public static Task<GettingStartedResponse> GettingStarted(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.GetGettingStartedAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Puts your getting-started card away. Nobody else's changes.</summary>
    [WolverinePost("/api/spaces/{spaceId}/getting-started/dismiss")]
    public static Task<GettingStartedResponse> DismissGettingStarted(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.DismissGettingStartedAsync(Sessions.RequireUserId(principal), spaceId, true, cancellationToken);

    /// <summary>Shows your getting-started card again.</summary>
    [WolverinePost("/api/spaces/{spaceId}/getting-started/show")]
    public static Task<GettingStartedResponse> ShowGettingStarted(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.DismissGettingStartedAsync(Sessions.RequireUserId(principal), spaceId, false, cancellationToken);
}
