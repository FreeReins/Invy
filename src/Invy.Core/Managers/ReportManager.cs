using System.Runtime.CompilerServices;
using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="IReportManager"/>
internal sealed class ReportManager(
    IItemAccess items,
    ITypeAccess types,
    IAttachmentAccess attachments,
    ValidationEngine validation,
    QueryEngine queryEngine,
    ReportEngine reports,
    TimeProvider clock) : IReportManager
{
    /// <summary>The most things the insurance inventory's page lists; its CSV has them all.</summary>
    public const int InsuranceItems = 5_000;

    /// <summary>How many items a CSV report reads at a time.</summary>
    public const int CsvBatch = 500;

    public async Task<ValueTotalsResponse> ValueTotalsAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var byPlace = await items.TotalsByPlaceAsync(spaceId, cancellationToken);
        var byType = await items.TotalsByTypeAsync(spaceId, cancellationToken);
        var places = (await items.FindManyAsync(spaceId, byPlace.Select(total => total.PlaceId).OfType<Guid>().ToList(), cancellationToken))
            .ToDictionary(item => item.Id, item => item.ToRef());
        var typeNames = (await types.ListAsync(spaceId, cancellationToken)).ToDictionary(type => type.Id, type => type.Name);

        return new ValueTotalsResponse(
            byType.Sum(total => total.Value),
            byType.Sum(total => total.Count),
            byType.Sum(total => total.Count - total.Valued),
            byPlace.Where(total => total.Count > 0 || total.Value > 0)
                .Select(total => new PlaceValue(total.PlaceId is Guid id ? places.GetValueOrDefault(id) : null, total.Count, total.Value))
                .OrderByDescending(total => total.Value).ThenBy(total => total.Place?.Name ?? "￿", StringComparer.Ordinal).ToList(),
            byType.Select(total => new TypeValue(
                    total.TypeId,
                    total.TypeId is Guid id ? typeNames.GetValueOrDefault(id, "A removed type") : "No type",
                    total.Count, total.Valued, total.Value))
                .OrderByDescending(total => total.Value).ThenBy(total => total.Name, StringComparer.Ordinal).ToList());
    }

    public async Task<InsuranceReportResponse> InsuranceAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var query = await QueryAsync(spaceId, string.Empty, "name", cancellationToken);
        var listed = new List<SearchResult>();
        var truncated = false;
        await foreach (var batch in SearchPages.AllAsync(items, attachments, spaceId, query, CsvBatch, cancellationToken))
        {
            listed.AddRange(batch.Where(Insured));
            if (listed.Count > InsuranceItems)
            {
                truncated = true;
                listed.RemoveRange(InsuranceItems, listed.Count - InsuranceItems);
                break;
            }
        }

        var rooms = listed.GroupBy(result => result.Chain.Count > 0 ? result.Chain[0].Id : (Guid?)null)
            .Select(group => new InsuranceRoom(
                group.First().Chain.FirstOrDefault(), group.Sum(result => result.Item.Value ?? 0), group.ToList()))
            .OrderBy(room => room.Room is null).ThenBy(room => room.Room?.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new InsuranceReportResponse(clock.GetUtcNow(), listed.Sum(result => result.Item.Value ?? 0), listed.Count, truncated, rooms);
    }

    public async Task<ReportFile> InsuranceCsvAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var query = await QueryAsync(spaceId, string.Empty, "name", cancellationToken);
        return await CsvAsync(space, "insurance inventory", query, Insured, cancellationToken);
    }

    public async Task<SearchResponse> MissingAsync(Guid userId, Guid spaceId, string? after, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var query = await QueryAsync(spaceId, "is:missing", "name", cancellationToken);
        return await SearchPages.RunAsync(items, spaceId, query, after, attachments, cancellationToken);
    }

    public async Task<ReportFile> MissingCsvAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var query = await QueryAsync(spaceId, "is:missing", "name", cancellationToken);
        return await CsvAsync(space, "missing items", query, _ => true, cancellationToken);
    }

    public async Task<ReportFile> ViewCsvAsync(Guid userId, Guid spaceId, Guid viewId, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var view = await validation.ViewAsync(spaceId, userId, viewId, cancellationToken);
        var query = await QueryAsync(spaceId, view.Query, view.Sort, cancellationToken);
        return await CsvAsync(space, view.Name, query, _ => true, cancellationToken);
    }

    /// <summary>What an insurance inventory lists: things, and containers worth something themselves.</summary>
    private static bool Insured(SearchResult result) => !result.Item.HoldsItems || result.Item.Value is not null;

    private async Task<ItemQuery> QueryAsync(Guid spaceId, string text, string? sort, CancellationToken cancellationToken) =>
        queryEngine.Build(text, await types.ListAsync(spaceId, cancellationToken), clock.GetUtcNow(), sort);

    /// <summary>The report's file, read and written a batch at a time as it is sent.</summary>
    private async Task<ReportFile> CsvAsync(
        Space space, string report, ItemQuery query, Func<SearchResult, bool> include, CancellationToken cancellationToken)
    {
        var spaceTypes = await types.ListAsync(space.Id, cancellationToken);
        var fields = reports.FieldColumns(spaceTypes);
        return new ReportFile(reports.FileName(space.Name, report, clock.GetUtcNow()), Lines(cancellationToken));

        async IAsyncEnumerable<string> Lines([EnumeratorCancellation] CancellationToken reading = default)
        {
            yield return reports.Header(fields);
            await foreach (var batch in SearchPages.AllAsync(items, attachments, space.Id, query, CsvBatch, reading))
            {
                foreach (var result in batch.Where(include))
                {
                    yield return reports.Row(result, spaceTypes, fields, photo => $"/api/spaces/{space.Id}/attachments/{photo}/file");
                }
            }
        }
    }
}
