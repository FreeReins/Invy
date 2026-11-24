using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="IItemManager"/>
internal sealed partial class ItemManager(
    IItemAccess items,
    ITypeAccess types,
    IAccountAccess accounts,
    ISettingsAccess settings,
    IProductAccess products,
    IAiAccess ai,
    IMessageAccess messages,
    ValidationEngine validation,
    ItemEngine itemEngine,
    TimelineEngine timeline,
    QuickAddEngine quickAdd,
    QueryEngine queryEngine,
    TimeProvider clock) : IItemManager
{
    /// <summary>How many items a page of contents holds.</summary>
    public const int PageSize = 100;

    public async Task<ItemDetailResponse> GetItemAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var ancestors = ItemPaths.AncestorIds(item.Path);
        var found = (await items.FindManyAsync(spaceId, ancestors, cancellationToken)).ToDictionary(ancestor => ancestor.Id);
        var chain = ancestors.Where(found.ContainsKey).Select(ancestorId => found[ancestorId].ToRef()).ToList();
        var contents = (await items.CountChildrenAsync(spaceId, [id], cancellationToken)).GetValueOrDefault(id);
        return new ItemDetailResponse(item.ToResponse(), chain, contents);
    }

    public async Task<ItemPageResponse> ListContentsAsync(Guid userId, Guid spaceId, Guid? parentId, string? after, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        if (parentId is Guid id)
        {
            await validation.ItemAsync(spaceId, id, cancellationToken);
        }

        var page = await items.ListChildrenAsync(spaceId, parentId, ValidationEngine.ItemCursor(after), PageSize + 1, cancellationToken);
        return await PageAsync(spaceId, page, cancellationToken);
    }

    public async Task<ItemPageResponse> ListInboxAsync(Guid userId, Guid spaceId, string? after, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var page = await items.ListInboxAsync(spaceId, ValidationEngine.ItemCursor(after), PageSize + 1, cancellationToken);
        return await PageAsync(spaceId, page, cancellationToken);
    }

    public async Task<ItemResponse> CreateItemAsync(Guid userId, Guid spaceId, ItemRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var type = await validation.ItemTypeAsync(spaceId, request.TypeId, cancellationToken);
        var parent = await validation.ContainerAsync(spaceId, request.ParentId, cancellationToken);
        var now = clock.GetUtcNow();
        var item = new Item { SpaceId = spaceId, Status = ItemStatus.Active, CreatedAt = now, UpdatedAt = now, Version = 1 };
        ValidationEngine.ItemDetails(item, request, type);
        item.Fields = await validation.FieldValuesAsync(spaceId, type, request.Fields, item.Fields, cancellationToken);
        Place(item, parent);

        await items.SaveAsync(item, Derived(item, type), cancellationToken);
        await items.AddEventAsync(Event(item, userId, ItemEventKind.Created, now, to: parent?.Id), cancellationToken);
        await RecountLaterAsync(spaceId, item.Path);
        await SuggestTypesLaterAsync(spaceId, [item], cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }

    public async Task<ItemResponse> UpdateItemAsync(Guid userId, Guid spaceId, Guid id, ItemRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var before = await validation.ItemAsync(spaceId, id, cancellationToken);
        if (request.Version != before.Version)
        {
            throw new ConflictException($"Someone else changed {before.Name} while you were editing it. Reload to see their change, then try again.");
        }

        var typeBefore = await validation.ItemTypeAsync(spaceId, before.TypeId, cancellationToken);
        var type = await validation.ItemTypeAsync(spaceId, request.TypeId, cancellationToken);
        var item = before.Copy();
        ValidationEngine.ItemDetails(item, request, type);
        item.Fields = await validation.FieldValuesAsync(spaceId, type, request.Fields, before.Fields, cancellationToken);
        if (item.TypeId is not null)
        {
            item.SuggestedTypeId = null;
        }

        if (before.HoldsItems && !item.HoldsItems)
        {
            await validation.EmptyAsync(item, "turn off Holds items", cancellationToken);
        }

        var now = clock.GetUtcNow();
        var changes = itemEngine.Changes(before, item, typeBefore, type).ToList();
        decimal? delta = null;
        if (before.Quantity is decimal oldAmount && item.Quantity is decimal newAmount)
        {
            delta = newAmount != oldAmount ? newAmount - oldAmount : null;
        }
        else if (before.Quantity != item.Quantity)
        {
            changes.Add(new ItemChange("Amount", Mapping.Amount(before), Mapping.Amount(item)));
        }

        if (changes.Count == 0 && delta is null)
        {
            return before.ToResponse();
        }

        // Editing an example makes it the person's own, so removing examples leaves it.
        item.IsExample = false;
        item.Version = before.Version + 1;
        item.UpdatedAt = now;
        await items.SaveAsync(item, Derived(item, type), cancellationToken);
        if (changes.Count > 0)
        {
            var changed = Event(item, userId, ItemEventKind.Changed, now);
            changed.Changes = changes;
            await items.AddEventAsync(changed, cancellationToken);
        }

        if (delta is not null)
        {
            var counted = Event(item, userId, ItemEventKind.QuantityChanged, now);
            counted.QtyDelta = delta;
            await items.AddEventAsync(counted, cancellationToken);
        }

        await RecountLaterAsync(spaceId, item.Path);
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }

    /// <summary>Works out the item's value and returns its sortable values, both from its fields and type.</summary>
    private IReadOnlyList<ItemValue> Derived(Item item, ItemType? type)
    {
        item.Value = itemEngine.Value(item, type);
        return itemEngine.SortableValues(item, type);
    }

    /// <summary>Sets the item's container, path and depth for being inside <paramref name="parent"/> (null for the top).</summary>
    private static void Place(Item item, Item? parent) => ItemEngine.Place(item, parent);

    private static ItemEvent Event(Item item, Guid userId, ItemEventKind kind, DateTimeOffset at, Guid? from = null, Guid? to = null) =>
        ItemEngine.Event(item, userId, kind, at, from, to);

    /// <summary>A page of items, with the counts the rollups queue keeps. The list holds one more than a page when there is a next one.</summary>
    private async Task<ItemPageResponse> PageAsync(Guid spaceId, List<Item> found, CancellationToken cancellationToken)
    {
        var page = found.Take(PageSize).ToList();
        var next = found.Count > PageSize ? Mapping.Cursor(page[^1]) : null;
        return new ItemPageResponse(page.Select(item => item.ToSummary()).ToList(), next);
    }
}
