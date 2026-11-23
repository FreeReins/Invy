using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>Photos and PDFs attached to items. Any member can look and download; editors and owners attach and remove.</summary>
public static class AttachmentEndpoints
{
    /// <summary>An item's attachments, oldest first.</summary>
    [WolverineGet("/api/spaces/{spaceId}/items/{id}/attachments")]
    public static Task<IReadOnlyList<AttachmentResponse>> List(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IAttachmentManager attachments, CancellationToken cancellationToken) =>
        attachments.ListAttachmentsAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>Attaches a photo or PDF (base64 in JSON, up to 10 MB) to an item.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/attachments")]
    public static Task<AttachmentResponse> Add(
        Guid spaceId, Guid id, AddAttachmentRequest request, ClaimsPrincipal principal, [FromServices] IAttachmentManager attachments,
        CancellationToken cancellationToken) =>
        attachments.AddAttachmentAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>Downloads a photo or PDF from the public internet, such as a looked-up product's photo, and attaches it.</summary>
    [WolverinePost("/api/spaces/{spaceId}/items/{id}/attachments/from-web")]
    public static Task<AttachmentResponse> AddFromWeb(
        Guid spaceId, Guid id, AttachFromWebRequest request, ClaimsPrincipal principal, [FromServices] IAttachmentManager attachments,
        CancellationToken cancellationToken) =>
        attachments.AttachFromWebAsync(Sessions.RequireUserId(principal), spaceId, id, request, cancellationToken);

    /// <summary>The attached file. Photos are shown in the page; PDFs are downloaded.</summary>
    [WolverineGet("/api/spaces/{spaceId}/attachments/{id}/file")]
    public static Task<IResult> File(
        Guid spaceId, Guid id, ClaimsPrincipal principal, HttpContext context, [FromServices] IAttachmentManager attachments,
        CancellationToken cancellationToken) =>
        SendAsync(spaceId, id, thumbnail: false, principal, context, attachments, cancellationToken);

    /// <summary>A photo's small copy, a JPEG at most 480 pixels across. 404 until it has been made.</summary>
    [WolverineGet("/api/spaces/{spaceId}/attachments/{id}/thumbnail")]
    public static Task<IResult> Thumbnail(
        Guid spaceId, Guid id, ClaimsPrincipal principal, HttpContext context, [FromServices] IAttachmentManager attachments,
        CancellationToken cancellationToken) =>
        SendAsync(spaceId, id, thumbnail: true, principal, context, attachments, cancellationToken);

    /// <summary>Removes an attachment.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/attachments/{id}")]
    public static Task Delete(
        Guid spaceId, Guid id, ClaimsPrincipal principal, [FromServices] IAttachmentManager attachments, CancellationToken cancellationToken) =>
        attachments.DeleteAttachmentAsync(Sessions.RequireUserId(principal), spaceId, id, cancellationToken);

    /// <summary>
    /// Sends the bytes with the type Invy worked out from them, never one a browser would run. Files never change, so
    /// they can be kept a day by the browser that asked; nothing in between keeps them.
    /// </summary>
    private static async Task<IResult> SendAsync(
        Guid spaceId, Guid id, bool thumbnail, ClaimsPrincipal principal, HttpContext context, IAttachmentManager attachments,
        CancellationToken cancellationToken)
    {
        var file = await attachments.OpenAttachmentAsync(Sessions.RequireUserId(principal), spaceId, id, thumbnail, cancellationToken);
        context.Response.Headers.CacheControl = "private, max-age=86400";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        var inline = file.ContentType.StartsWith("image/", StringComparison.Ordinal);
        return Results.File(file.Content, file.ContentType, inline ? null : file.Name, enableRangeProcessing: true);
    }
}
