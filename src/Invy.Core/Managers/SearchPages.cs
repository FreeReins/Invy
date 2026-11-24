using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <summary>A page of search results with where each item is, shared by searches and views.</summary>
internal static class SearchPages
{
    /// <summary>How many results a page holds.</summary>
    public const int PageSize = 50;

    /// <summary>Above this many results the total is the database's estimate.</summary>
    public const int ExactCountCap = 10_000;

    /// <summary>
    /// The page after <paramref name="after"/> (a last page's Next), with the total on the first page. With
    /// <paramref name="photos"/> (a view), each result carries its details too.
    /// </summary>
    public static async Task<SearchResponse> RunAsync(
        IItemAccess items, Guid spaceId, ItemQuery query, string? after, IAttachmentAccess? photos, CancellationToken cancellationToken)
    {
        var hits = await items.SearchAsync(spaceId, query, ValidationEngine.SearchCursor(after), PageSize + 1, cancellationToken);
        var page = hits.Take(PageSize).ToList();
        var results = await ResultsAsync(items, photos, spaceId, page, cancellationToken);

        var total = after is null ? await items.CountAsync(spaceId, query, ExactCountCap, cancellationToken) : null;
        var next = hits.Count > PageSize ? Mapping.Cursor(page[^1]) : null;
        return new SearchResponse(results, total?.Count, total?.Estimated ?? false, next);
    }

    /// <summary>
    /// Every item matching <paramref name="query"/>, in its order, a batch at a time, each with its details: for
    /// reports, which read a whole space without holding it in memory.
    /// </summary>
    public static async IAsyncEnumerable<List<SearchResult>> AllAsync(
        IItemAccess items, IAttachmentAccess photos, Guid spaceId, ItemQuery query, int batch,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SearchHit? after = null;
        while (true)
        {
            var hits = await items.SearchAsync(spaceId, query, after, batch, cancellationToken);
            if (hits.Count == 0)
            {
                yield break;
            }

            yield return await ResultsAsync(items, photos, spaceId, hits, cancellationToken);
            if (hits.Count < batch)
            {
                yield break;
            }

            after = hits[^1];
        }
    }

    /// <summary>The items of these hits, in their order, with where each is (and its details when <paramref name="photos"/> is given).</summary>
    private static async Task<List<SearchResult>> ResultsAsync(
        IItemAccess items, IAttachmentAccess? photos, Guid spaceId, IReadOnlyList<SearchHit> hits, CancellationToken cancellationToken)
    {
        var found = (await items.FindManyAsync(spaceId, hits.Select(hit => hit.Id).ToList(), cancellationToken)).ToDictionary(item => item.Id);
        var ancestors = found.Values.SelectMany(item => ItemPaths.AncestorIds(item.Path)).Distinct().ToList();
        var containers = (await items.FindManyAsync(spaceId, ancestors, cancellationToken)).ToDictionary(item => item.Id);
        var firstPhotos = photos is null ? null : await photos.FirstPhotosAsync(spaceId, found.Keys, cancellationToken);
        return hits.Where(hit => found.ContainsKey(hit.Id)).Select(hit => found[hit.Id]).Select(item => new SearchResult(
            item.ToSummary(),
            ItemPaths.AncestorIds(item.Path).Where(containers.ContainsKey).Select(id => containers[id].ToRef()).ToList(),
            firstPhotos is null ? null : item.ToDetails(firstPhotos.TryGetValue(item.Id, out var photo) ? photo : null))).ToList();
    }
}
