using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// Reports on a space: value totals, an insurance inventory, the missing items audits found, and a CSV of any view.
/// The page hides each behind its feature (values, audits), but they answer whatever the switches say.
/// </summary>
public interface IReportManager
{
    /// <summary>What the space's things are worth, in total, by place at the top (from the rollups) and by type.</summary>
    Task<ValueTotalsResponse> ValueTotalsAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>The insurance inventory by room, up to <c>InsuranceItems</c> things; the CSV has them all.</summary>
    Task<InsuranceReportResponse> InsuranceAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>The insurance inventory as CSV: every thing, with its room, place, value, fields and photo address.</summary>
    Task<ReportFile> InsuranceCsvAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>The items marked missing (by an audit, or by hand), by name, 50 at a time, with where they belong.</summary>
    Task<SearchResponse> MissingAsync(Guid userId, Guid spaceId, string? after, CancellationToken cancellationToken);

    /// <summary>The missing items as CSV.</summary>
    Task<ReportFile> MissingCsvAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>Everything a view finds as of now, as CSV, in the view's order.</summary>
    /// <exception cref="RuleViolationException">Its search no longer reads; the message says which term.</exception>
    Task<ReportFile> ViewCsvAsync(Guid userId, Guid spaceId, Guid viewId, CancellationToken cancellationToken);
}
