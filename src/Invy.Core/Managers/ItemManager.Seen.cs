using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Last seen and audit walks.</content>
internal sealed partial class ItemManager
{
    /// <summary>The most items an audit checks in one container.</summary>
    public const int MaxAuditItems = 2_000;

    public async Task<ItemResponse> MarkSeenAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var now = clock.GetUtcNow();
        if (item.LastSeenAt is DateTimeOffset last && now - last < IItemManager.SeenAgainAfter)
        {
            return item.ToResponse();
        }

        // One event for the container; what's inside only gets its time, in the same statement.
        await items.MarkSeenAsync(spaceId, [id], item.HoldsItems ? id : null, now, cancellationToken);
        await items.AddEventAsync(Event(item, userId, ItemEventKind.Seen, now), cancellationToken);

        // Sent even for an item at the top (no containers to recount), so open pages showing it, such as a "not seen
        // in a year" view, load again through the items live topic.
        await messages.PublishAsync(new ItemsChanged(spaceId, ItemPaths.AncestorIds(item.Path).ToList()));
        await messages.CommitAsync(cancellationToken);
        item.LastSeenAt = now;
        return item.ToResponse();
    }

    public async Task<AuditResponse> AuditAsync(Guid userId, Guid spaceId, Guid id, AuditRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var container = await validation.ContainerAsync(spaceId, id, cancellationToken)
            ?? throw new NotFoundException("That container no longer exists. Pick another place.");
        var present = (request.Present ?? []).Distinct().Where(presentId => presentId != id).ToHashSet();
        var contents = await items.ListChildrenAsync(spaceId, id, null, MaxAuditItems + 1, cancellationToken);
        if (contents.Count > MaxAuditItems || present.Count > MaxAuditItems)
        {
            throw new RuleViolationException(
                $"An audit checks up to {MaxAuditItems} items at a time, and {container.Name} holds more. Audit the containers inside it instead.");
        }

        var inside = contents.Select(item => item.Id).ToHashSet();
        var otherIds = present.Where(presentId => !inside.Contains(presentId)).ToList();
        var elsewhere = await items.FindManyAsync(spaceId, otherIds, cancellationToken);
        if (elsewhere.Count != otherIds.Count)
        {
            throw new NotFoundException("Some of the ticked items no longer exist. Reload the audit and try again.");
        }

        var now = clock.GetUtcNow();
        var missing = new List<Item>();
        var found = new List<Item>();
        foreach (var item in contents.Concat(elsewhere))
        {
            var ticked = present.Contains(item.Id);
            if (ticked && item.Status == ItemStatus.Missing)
            {
                await ChangeStatusAsync(item, userId, ItemStatus.Active, ItemEventKind.Found, now, cancellationToken);
                found.Add(item);
            }
            else if (!ticked && item.Status == ItemStatus.Active)
            {
                await ChangeStatusAsync(item, userId, ItemStatus.Missing, ItemEventKind.Missing, now, cancellationToken);
                missing.Add(item);
            }
            else if (!ticked && item.Status == ItemStatus.Missing)
            {
                missing.Add(item);
            }
        }

        await items.MarkSeenAsync(spaceId, [id, .. present], null, now, cancellationToken);
        await items.AddEventAsync(Event(container, userId, ItemEventKind.Seen, now), cancellationToken);
        await RecountLaterAsync(spaceId, container.Path);
        await messages.CommitAsync(cancellationToken);
        container.LastSeenAt = now;
        return new AuditResponse(
            container.ToResponse(),
            present.Count,
            missing.Select(item => item.ToSummary()).ToList(),
            found.Select(item => item.ToSummary()).ToList(),
            elsewhere.Where(item => item.ParentId != id).Select(item => item.ToSummary()).ToList());
    }

    /// <summary>Saves a status change with its event, without committing: an audit changes many items at once.</summary>
    private async Task ChangeStatusAsync(
        Item item, Guid userId, ItemStatus status, ItemEventKind kind, DateTimeOffset now, CancellationToken cancellationToken)
    {
        item.Status = status;
        item.Version++;
        item.UpdatedAt = now;
        if (kind == ItemEventKind.Found)
        {
            item.LastSeenAt = now;
        }

        await items.SaveAsync(item, await ValuesAsync(item, cancellationToken), cancellationToken);
        await items.AddEventAsync(Event(item, userId, kind, now), cancellationToken);
    }
}
