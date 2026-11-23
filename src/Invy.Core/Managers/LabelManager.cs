using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="ILabelManager"/>
internal sealed class LabelManager(
    ILabelAccess labels,
    IItemAccess items,
    IMessageAccess messages,
    ValidationEngine validation,
    LabelEngine labelEngine,
    ItemEngine itemEngine,
    TimeProvider clock) : ILabelManager
{
    /// <summary>How many labels a page holds.</summary>
    public const int PageSize = 100;

    /// <summary>How many times to draw fresh codes for a batch before making them a character longer.</summary>
    private const int Draws = 5;

    public async Task<IReadOnlyList<LabelResponse>> CreateLabelsAsync(
        Guid userId, Guid spaceId, CreateLabelsRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var count = ValidationEngine.LabelCount(request.Count);
        var length = labelEngine.LengthFor(await labels.CountAsync(cancellationToken));
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        for (var draw = 0; codes.Count < count; draw++)
        {
            if (draw > 0 && draw % Draws == 0)
            {
                length = Math.Min(length + 1, LabelEngine.MaxLength);
            }

            var drawn = labelEngine.NewCodes(count - codes.Count, length).Where(code => !codes.Contains(code)).ToList();
            var taken = await labels.ExistingAsync(drawn, cancellationToken);
            codes.UnionWith(drawn.Where(code => !taken.Contains(code)));
        }

        var now = clock.GetUtcNow();
        var made = new List<Label>();
        foreach (var code in codes)
        {
            var label = new Label { Code = code, SpaceId = spaceId, CreatedAt = now };
            await labels.SaveAsync(label, cancellationToken);
            made.Add(label);
        }

        await messages.CommitAsync(cancellationToken);
        return made.Select(label => label.ToResponse(null)).ToList();
    }

    public async Task<LabelPageResponse> ListLabelsAsync(Guid userId, Guid spaceId, bool unclaimed, string? after, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var found = await labels.ListAsync(spaceId, unclaimed, after, PageSize + 1, cancellationToken);
        var page = found.Take(PageSize).ToList();
        var itemIds = page.Where(label => label.ItemId is not null).Select(label => label.ItemId!.Value).ToList();
        var onItems = itemIds.Count == 0
            ? []
            : (await items.FindManyAsync(spaceId, itemIds, cancellationToken)).ToDictionary(item => item.Id, item => item.ToRef());
        return new LabelPageResponse(
            page.Select(label => label.ToResponse(label.ItemId is Guid id ? onItems.GetValueOrDefault(id) : null)).ToList(),
            found.Count > PageSize ? page[^1].Code : null);
    }

    public async Task<LabelResponse> FindLabelAsync(Guid userId, string text, CancellationToken cancellationToken)
    {
        var label = await validation.LabelAsync(labelEngine.Code(text), cancellationToken);
        await validation.SpaceRoleAsync(label.SpaceId, userId, SpaceRole.Viewer, cancellationToken);
        return label.ToResponse(await OnItemAsync(label, cancellationToken));
    }

    public async Task<ItemResponse> ClaimLabelAsync(
        Guid userId, Guid spaceId, string code, ClaimLabelRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var label = await validation.LabelAsync(spaceId, labelEngine.Code(code), cancellationToken);
        if (await OnItemAsync(label, cancellationToken) is { } on)
        {
            throw new RuleViolationException($"Label {label.Code} is already on {on.Name}. Open it instead.");
        }

        var type = await validation.ItemTypeAsync(spaceId, request.TypeId, cancellationToken);
        var parent = await validation.ContainerAsync(spaceId, request.ParentId, cancellationToken);
        var now = clock.GetUtcNow();
        var item = new Item
        {
            SpaceId = spaceId, LabelCode = label.Code, Status = ItemStatus.Active, CreatedAt = now, UpdatedAt = now, Version = 1,
        };
        var details = new ItemRequest(
            string.IsNullOrWhiteSpace(request.Name) ? labelEngine.DefaultName(label.Code) : request.Name,
            null, request.TypeId, request.ParentId, type is null ? true : null, null, null, null, null, null, 0);
        ValidationEngine.ItemDetails(item, details, type);
        item.Fields = await validation.FieldValuesAsync(spaceId, type, null, item.Fields, cancellationToken);
        ItemEngine.Place(item, parent);
        item.Value = itemEngine.Value(item, type);

        await items.SaveAsync(item, itemEngine.SortableValues(item, type), cancellationToken);
        await items.AddEventAsync(ItemEngine.Event(item, userId, ItemEventKind.Created, now, to: parent?.Id), cancellationToken);
        label.ItemId = item.Id;
        await labels.SaveAsync(label, cancellationToken);
        if (ItemPaths.AncestorIds(item.Path) is { Count: > 0 } containers)
        {
            await messages.PublishAsync(new ItemsChanged(spaceId, containers));
        }

        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }

    public async Task<ItemResponse> AttachLabelAsync(
        Guid userId, Guid spaceId, string code, AttachLabelRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var label = await validation.LabelAsync(spaceId, labelEngine.Code(code), cancellationToken);
        var before = await validation.ItemAsync(spaceId, request.ItemId, cancellationToken);
        if (label.ItemId == before.Id)
        {
            return before.ToResponse();
        }

        if (await OnItemAsync(label, cancellationToken) is { } on)
        {
            throw new RuleViolationException($"Label {label.Code} is already on {on.Name}. Pick an unused label.");
        }

        if (await labels.FindForItemAsync(spaceId, before.Id, cancellationToken) is { } old)
        {
            old.ItemId = null;
            await labels.SaveAsync(old, cancellationToken);
        }

        var now = clock.GetUtcNow();
        var type = await validation.ItemTypeAsync(spaceId, before.TypeId, cancellationToken);
        var item = before.Copy();
        item.LabelCode = label.Code;
        item.Version = before.Version + 1;
        item.UpdatedAt = now;
        await items.SaveAsync(item, itemEngine.SortableValues(item, type), cancellationToken);
        var changed = ItemEngine.Event(item, userId, ItemEventKind.Changed, now);
        changed.Changes = [new ItemChange("Label", before.LabelCode, label.Code)];
        await items.AddEventAsync(changed, cancellationToken);
        label.ItemId = item.Id;
        await labels.SaveAsync(label, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse();
    }

    /// <summary>The item a label is on, or null while it is unclaimed (or its item was removed).</summary>
    private async Task<ItemRef?> OnItemAsync(Label label, CancellationToken cancellationToken) =>
        label.ItemId is Guid id && await items.FindAsync(label.SpaceId, id, cancellationToken) is { } item ? item.ToRef() : null;
}
