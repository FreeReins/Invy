using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>Imports from CSV files and Homebox's export. Editors upload, check the columns and start; it runs in the background.</summary>
public static class ImportEndpoints
{
    /// <summary>The space's imports, newest first.</summary>
    [WolverineGet("/api/spaces/{spaceId}/imports")]
    public static Task<IReadOnlyList<ImportResponse>> List(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] IImportManager imports, CancellationToken cancellationToken) =>
        imports.ListImportsAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Uploads a CSV file (base64 in JSON, up to 20 MB) and suggests where each column goes. Nothing is added yet.</summary>
    [WolverinePost("/api/spaces/{spaceId}/imports")]
    public static Task<ImportResponse> Upload(
        Guid spaceId, UploadImportRequest request, ClaimsPrincipal principal, [FromServices] IImportManager imports,
        CancellationToken cancellationToken) =>
        imports.UploadAsync(Sessions.RequireUserId(principal), spaceId, request, cancellationToken);

    /// <summary>One import: its columns, first rows, mapping and progress.</summary>
    [WolverineGet("/api/spaces/{spaceId}/imports/{importId}")]
    public static Task<ImportResponse> Get(
        Guid spaceId, Guid importId, ClaimsPrincipal principal, [FromServices] IImportManager imports, CancellationToken cancellationToken) =>
        imports.GetImportAsync(Sessions.RequireUserId(principal), spaceId, importId, cancellationToken);

    /// <summary>Starts an uploaded import with its columns mapped.</summary>
    [WolverinePost("/api/spaces/{spaceId}/imports/{importId}/start")]
    public static Task<ImportResponse> Start(
        Guid spaceId, Guid importId, StartImportRequest request, ClaimsPrincipal principal, [FromServices] IImportManager imports,
        CancellationToken cancellationToken) =>
        imports.StartImportAsync(Sessions.RequireUserId(principal), spaceId, importId, request, cancellationToken);

    /// <summary>Removes an import that isn't running. What it added stays.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/imports/{importId}")]
    public static Task Delete(
        Guid spaceId, Guid importId, ClaimsPrincipal principal, [FromServices] IImportManager imports, CancellationToken cancellationToken) =>
        imports.DeleteImportAsync(Sessions.RequireUserId(principal), spaceId, importId, cancellationToken);
}
