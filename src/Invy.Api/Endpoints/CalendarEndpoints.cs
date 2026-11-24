using System.Security.Claims;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>
/// A space's calendar feed of reminder and loan dates. Calendar apps can't sign in, so the feed's address carries a
/// secret instead; members see the address, and owners turn the feed on, replace its address or turn it off.
/// </summary>
public static class CalendarEndpoints
{
    /// <summary>The feed itself, as iCalendar text. Needs no sign-in, only the secret. 404 when it is off or the secret is wrong.</summary>
    [AllowAnonymous]
    [WolverineGet("/api/spaces/{spaceId}/calendar/{token}.ics")]
    public static async Task<IResult> Feed(
        Guid spaceId, string token, HttpContext http, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken)
    {
        var text = await spaces.GetCalendarAsync(spaceId, token, Site(http), cancellationToken);
        http.Response.Headers.CacheControl = "private, max-age=300";
        return Results.Text(text, "text/calendar; charset=utf-8");
    }

    /// <summary>The feed's address to subscribe to, or <c>{"url": null}</c> while it is off.</summary>
    [WolverineGet("/api/spaces/{spaceId}/calendar")]
    public static Task<CalendarFeedResponse> Get(
        Guid spaceId, HttpContext http, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.GetCalendarFeedAsync(Sessions.RequireUserId(principal), spaceId, Site(http), cancellationToken);

    /// <summary>Turns the feed on with a new address; one made before stops working. Owners only.</summary>
    [WolverinePost("/api/spaces/{spaceId}/calendar")]
    public static Task<CalendarFeedResponse> Start(
        Guid spaceId, HttpContext http, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.StartCalendarFeedAsync(Sessions.RequireUserId(principal), spaceId, Site(http), cancellationToken);

    /// <summary>Turns the feed off. Owners only.</summary>
    [WolverineDelete("/api/spaces/{spaceId}/calendar")]
    public static Task<CalendarFeedResponse> Stop(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] ISpaceManager spaces, CancellationToken cancellationToken) =>
        spaces.StopCalendarFeedAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>Invy's address as the request reached it (after a trusted proxy's forwarded headers), ending in a slash.</summary>
    private static Uri Site(HttpContext http) => new($"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}/");
}
