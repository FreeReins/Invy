using System.Security.Claims;
using System.Text;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Wolverine.Http;

namespace Invy.Api.Endpoints;

/// <summary>
/// Reports on a space: value totals, an insurance inventory, the missing items audits found, and a CSV of any view.
/// Their PDF is the page's own print (docs/developers/design.md, Ideas by audience); CSV files come from here.
/// </summary>
public static class ReportEndpoints
{
    /// <summary>What the space's things are worth, in total, by place at the top and by type.</summary>
    [WolverineGet("/api/spaces/{spaceId}/reports/values")]
    public static Task<ValueTotalsResponse> Values(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] IReportManager reports, CancellationToken cancellationToken) =>
        reports.ValueTotalsAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>The insurance inventory by room, with photos and values.</summary>
    [WolverineGet("/api/spaces/{spaceId}/reports/insurance")]
    public static Task<InsuranceReportResponse> Insurance(
        Guid spaceId, ClaimsPrincipal principal, [FromServices] IReportManager reports, CancellationToken cancellationToken) =>
        reports.InsuranceAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken);

    /// <summary>The insurance inventory as CSV, every thing in the space.</summary>
    [WolverineGet("/api/spaces/{spaceId}/reports/insurance.csv")]
    public static async Task InsuranceCsv(
        Guid spaceId, ClaimsPrincipal principal, HttpContext http, [FromServices] IReportManager reports, CancellationToken cancellationToken) =>
        await WriteCsvAsync(http, await reports.InsuranceCsvAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken), cancellationToken);

    /// <summary>The items marked missing, by name, 50 at a time; <c>after</c> is the last page's <c>next</c>.</summary>
    [WolverineGet("/api/spaces/{spaceId}/reports/missing")]
    public static Task<SearchResponse> Missing(
        Guid spaceId, string? after, ClaimsPrincipal principal, [FromServices] IReportManager reports, CancellationToken cancellationToken) =>
        reports.MissingAsync(Sessions.RequireUserId(principal), spaceId, after, cancellationToken);

    /// <summary>The missing items as CSV.</summary>
    [WolverineGet("/api/spaces/{spaceId}/reports/missing.csv")]
    public static async Task MissingCsv(
        Guid spaceId, ClaimsPrincipal principal, HttpContext http, [FromServices] IReportManager reports, CancellationToken cancellationToken) =>
        await WriteCsvAsync(http, await reports.MissingCsvAsync(Sessions.RequireUserId(principal), spaceId, cancellationToken), cancellationToken);

    /// <summary>Everything a view finds as of now, as CSV.</summary>
    [WolverineGet("/api/spaces/{spaceId}/views/{viewId}/export.csv")]
    public static async Task ViewCsv(
        Guid spaceId, Guid viewId, ClaimsPrincipal principal, HttpContext http, [FromServices] IReportManager reports, CancellationToken cancellationToken) =>
        await WriteCsvAsync(http, await reports.ViewCsvAsync(Sessions.RequireUserId(principal), spaceId, viewId, cancellationToken), cancellationToken);

    /// <summary>
    /// Sends the report as it is read, inside the request so its database work shares the request's scope. UTF-8 with
    /// a byte order mark, so spreadsheets read its accents.
    /// </summary>
    private static async Task WriteCsvAsync(HttpContext http, ReportFile file, CancellationToken cancellationToken)
    {
        http.Response.ContentType = "text/csv; charset=utf-8";
        http.Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = file.Name,
            FileNameStar = file.Name,
        }.ToString();
        await using var writer = new StreamWriter(http.Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await foreach (var line in file.Lines.WithCancellation(cancellationToken))
        {
            await writer.WriteAsync(line);
        }
    }
}
