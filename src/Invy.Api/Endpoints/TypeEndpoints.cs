using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>A space's item types and their custom fields. Any member can look; editors and owners change them.</summary>
public static class TypeEndpoints
{
    /// <summary>The space's types, by name.</summary>
    [WolverineGet("/api/spaces/{spaceId}/types")]
    public static Task<IReadOnlyList<ItemTypeResponse>> List(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] ITypeManager types, CancellationToken cancellationToken) =>
        types.ListTypesAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>One type with its fields.</summary>
    [WolverineGet("/api/spaces/{spaceId}/types/{typeId}")]
    public static Task<ItemTypeResponse> Get(
        Guid spaceId, Guid typeId, ClaimsPrincipal principal, [FromServices] ITypeManager types, CancellationToken cancellationToken) =>
        types.GetTypeAsync(Sessions.RequireUserId(principal), spaceId, typeId, cancellationToken);

    /// <summary>Adds a type.</summary>
    [WolverinePost("/api/spaces/{spaceId}/types")]
    public static Task<ItemTypeResponse> Create(
        Guid spaceId, ItemTypeRequest request, ClaimsPrincipal principal, [FromServices] ITypeManager types,
        CancellationToken cancellationToken) =>
        types.CreateTypeAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>Replaces a type's name, look and fields. Send the version you opened; 409 if someone changed it since.</summary>
    [WolverinePut("/api/spaces/{spaceId}/types/{typeId}")]
    public static Task<ItemTypeResponse> Update(
        Guid spaceId, Guid typeId, ItemTypeRequest request, ClaimsPrincipal principal, [FromServices] ITypeManager types,
        CancellationToken cancellationToken) =>
        types.UpdateTypeAsync(Sessions.RequireUserId(principal), spaceId, typeId, request, cancellationToken);

    /// <summary>Removes a type. Its items keep their values.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/types/{typeId}")]
    public static Task Delete(
        Guid spaceId, Guid typeId, ClaimsPrincipal principal, [FromServices] ITypeManager types, CancellationToken cancellationToken) =>
        types.DeleteTypeAsync(Sessions.RequireUserId(principal), spaceId, typeId, cancellationToken);
}
