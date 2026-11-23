using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Search, barcode lookup, and keeping containers' counts and value totals up to date.</content>
internal sealed partial class ItemManager
{
    public async Task<ProductResponse> LookUpBarcodeAsync(Guid userId, Guid spaceId, string barcode, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var code = ValidationEngine.Barcode(barcode);
        if (!(await settings.GetSecurityAsync(cancellationToken)).BarcodeLookup)
        {
            throw new RuleViolationException("Barcode lookup is off. An admin can turn it on in Settings, Labels and printing; until then, type the name.");
        }

        var found = await products.LookUpAsync(code, cancellationToken)
            ?? throw new NotFoundException($"No product database knows barcode {code}. Type its name instead.");
        return new ProductResponse(code, found.Name, found.Brand, found.ImageUrl?.ToString(), found.Source);
    }

    /// <summary>How many results a page of search holds.</summary>
    public const int SearchPageSize = 50;

    /// <summary>Above this many results the total is the database's estimate.</summary>
    public const int ExactCountCap = 10_000;

    public async Task<SearchResponse> SearchAsync(
        Guid userId, Guid spaceId, string? text, string? sort, string? after, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var query = queryEngine.Build(text, await types.ListAsync(spaceId, cancellationToken), clock.GetUtcNow(), sort);
        var hits = await items.SearchAsync(spaceId, query, ValidationEngine.SearchCursor(after), SearchPageSize + 1, cancellationToken);
        var page = hits.Take(SearchPageSize).ToList();

        var found = (await items.FindManyAsync(spaceId, page.Select(hit => hit.Id).ToList(), cancellationToken)).ToDictionary(item => item.Id);
        var ancestors = found.Values.SelectMany(item => ItemPaths.AncestorIds(item.Path)).Distinct().ToList();
        var containers = (await items.FindManyAsync(spaceId, ancestors, cancellationToken)).ToDictionary(item => item.Id);
        var results = page.Where(hit => found.ContainsKey(hit.Id)).Select(hit => found[hit.Id]).Select(item => new SearchResult(
            item.ToSummary(),
            ItemPaths.AncestorIds(item.Path).Where(containers.ContainsKey).Select(id => containers[id].ToRef()).ToList())).ToList();

        var total = after is null ? await items.CountAsync(spaceId, query, ExactCountCap, cancellationToken) : null;
        var next = hits.Count > SearchPageSize ? Mapping.Cursor(page[^1]) : null;
        return new SearchResponse(results, total?.Count, total?.Estimated ?? false, next);
    }

    public async Task UpdateRollupsAsync(ItemsChanged message, CancellationToken cancellationToken)
    {
        await items.RecountAsync(message.SpaceId, message.ContainerIds, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    /// <summary>Asks for the containers of the items on these paths (not the items themselves) to be recounted, after the commit.</summary>
    private Task RecountLaterAsync(Guid spaceId, params string[] itemPaths) =>
        RecountLaterAsync(spaceId, itemPaths.SelectMany(ItemPaths.AncestorIds));

    /// <summary>Asks for these containers to be recounted, after the commit.</summary>
    private async Task RecountLaterAsync(Guid spaceId, IEnumerable<Guid> containerIds)
    {
        var containers = containerIds.Distinct().ToList();
        if (containers.Count > 0)
        {
            await messages.PublishAsync(new ItemsChanged(spaceId, containers));
        }
    }
}
