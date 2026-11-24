using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Moving items, changing amounts, disposing and restoring, and quick add.</content>
internal sealed partial class ItemManager
{
    public async Task<ItemResponse> MoveItemAsync(Guid userId, Guid spaceId, Guid id, MoveItemRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var target = await validation.ContainerAsync(spaceId, request.ParentId, cancellationToken);
        var moved = await MoveAsync(item, target, userId, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return moved.ToResponse();
    }

    public async Task<IReadOnlyList<ItemResponse>> MoveItemsAsync(Guid userId, Guid spaceId, MoveItemsRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var ids = (request.ItemIds ?? []).Distinct().ToList();
        if (ids.Count is 0 or > MaxMovedAtOnce)
        {
            throw new RuleViolationException($"Move 1 to {MaxMovedAtOnce} items at a time.");
        }

        var target = await validation.ContainerAsync(spaceId, request.ParentId, cancellationToken);
        var chosen = new List<Item>();
        foreach (var id in ids)
        {
            chosen.Add(await validation.ItemAsync(spaceId, id, cancellationToken));
        }

        // Something inside another chosen item goes with it, so only the outermost ones are moved.
        var outermost = chosen.Where(item => !ItemPaths.AncestorIds(item.Path).Any(ids.Contains)).ToList();
        var moved = new List<Item>();
        foreach (var item in outermost)
        {
            moved.Add(await MoveAsync(item, target, userId, cancellationToken));
        }

        await messages.CommitAsync(cancellationToken);
        return moved.Select(item => item.ToResponse()).ToList();
    }

    /// <summary>The most items <see cref="MoveItemsAsync"/> moves in one go.</summary>
    public const int MaxMovedAtOnce = 100;

    /// <summary>Moves one item and what's inside it, without committing. Writes one Moved event; nothing when it is already there.</summary>
    private async Task<Item> MoveAsync(Item item, Item? target, Guid userId, CancellationToken cancellationToken)
    {
        ValidationEngine.MoveTarget(item, target);
        if (item.ParentId == target?.Id)
        {
            return item;
        }

        var now = clock.GetUtcNow();
        var from = item.ParentId;
        var oldPath = item.Path;
        Place(item, target);
        item.Version++;
        item.UpdatedAt = now;

        // The item is saved as usual; what's inside it follows in one statement, with no event of its own.
        await items.MoveContentsAsync(item.SpaceId, oldPath, item.Path, cancellationToken);
        await items.SaveAsync(item, await ValuesAsync(item, cancellationToken), cancellationToken);
        await items.AddEventAsync(Event(item, userId, ItemEventKind.Moved, now, from, target?.Id), cancellationToken);
        await RecountLaterAsync(item.SpaceId, oldPath, item.Path);
        return item;
    }

    public async Task<ItemResponse> AdjustQuantityAsync(
        Guid userId, Guid spaceId, Guid id, AdjustQuantityRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        if (item.Quantity is not decimal amount)
        {
            throw new RuleViolationException($"{item.Name} is a single item, not counted. Edit it and give it an amount first.");
        }

        if (amount + request.Delta < 0)
        {
            throw new RuleViolationException($"There {(amount == 1 ? "is" : "are")} only {Mapping.Amount(item)} of {item.Name}, so that many can't be taken.");
        }

        var now = clock.GetUtcNow();
        item.Quantity = ValidationEngine.Quantity(amount + request.Delta);
        if (item.Quantity == amount)
        {
            return item.ToResponse();
        }

        item.Version++;
        item.UpdatedAt = now;
        var counted = Event(item, userId, ItemEventKind.QuantityChanged, now);
        counted.QtyDelta = item.Quantity - amount;
        await items.SaveAsync(item, await ValuesAsync(item, cancellationToken), cancellationToken);
        await items.AddEventAsync(counted, cancellationToken);
        await RecountLaterAsync(spaceId, item.Path);
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }

    public async Task<ItemResponse> DisposeItemAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        if (item.Status == ItemStatus.Disposed)
        {
            return item.ToResponse();
        }

        await validation.EmptyAsync(item, "dispose of it", cancellationToken);
        return await SetStatusAsync(item, userId, ItemStatus.Disposed, ItemEventKind.Disposed, cancellationToken);
    }

    public async Task<ItemResponse> RestoreItemAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        return item.Status is ItemStatus.Disposed or ItemStatus.Archived
            ? await SetStatusAsync(item, userId, ItemStatus.Active, ItemEventKind.Restored, cancellationToken)
            : item.ToResponse();
    }

    public async Task<IReadOnlyList<ItemResponse>> QuickAddAsync(
        Guid userId, Guid spaceId, QuickAddRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var parent = await validation.ContainerAsync(spaceId, request.ParentId, cancellationToken);
        var parsed = quickAdd.Parse(request.Text);
        if (parsed.Count == 0)
        {
            throw new RuleViolationException("Type at least one name, such as: baseball, glove, 2 bats.");
        }

        if (parsed.Count > QuickAddEngine.MaxItems)
        {
            throw new RuleViolationException($"Quick add makes up to {QuickAddEngine.MaxItems} items at a time. Split the list.");
        }

        var now = clock.GetUtcNow();
        var made = new List<Item>();
        foreach (var entry in parsed)
        {
            var item = new Item { SpaceId = spaceId, Status = ItemStatus.Active, CreatedAt = now, UpdatedAt = now, Version = 1 };
            ValidationEngine.ItemDetails(item, new ItemRequest(entry.Name, null, null, null, null, entry.Quantity, entry.Unit, null, null, null, 0), null);
            Place(item, parent);
            await items.SaveAsync(item, [], cancellationToken);
            await items.AddEventAsync(Event(item, userId, ItemEventKind.Created, now, to: parent?.Id), cancellationToken);
            made.Add(item);
        }

        if (parent is not null)
        {
            await RecountLaterAsync(spaceId, ItemPaths.Ids(parent.Path));
        }

        await SuggestTypesLaterAsync(spaceId, made, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return made.Select(item => item.ToResponse()).ToList();
    }

    private async Task<ItemResponse> SetStatusAsync(
        Item item, Guid userId, ItemStatus status, ItemEventKind kind, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        item.Status = status;
        item.Version++;
        item.UpdatedAt = now;
        await items.SaveAsync(item, await ValuesAsync(item, cancellationToken), cancellationToken);
        await items.AddEventAsync(Event(item, userId, kind, now), cancellationToken);
        await RecountLaterAsync(item.SpaceId, item.Path);
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }

    /// <summary>The item's value and sortable values, for a save that doesn't change its fields (an amount may change).</summary>
    private async Task<IReadOnlyList<ItemValue>> ValuesAsync(Item item, CancellationToken cancellationToken) =>
        Derived(item, await validation.ItemTypeAsync(item.SpaceId, item.TypeId, cancellationToken));
}
