using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <content>Types an AI model suggested for Inbox items.</content>
internal sealed partial class ItemAccess
{
    public async Task<List<Item>> ListInboxToSuggestAsync(Guid spaceId, DateTimeOffset? askedBefore, int limit, CancellationToken cancellationToken)
    {
        var before = askedBefore?.ToUniversalTime();
        return (await Current(spaceId)
                .Where(item => item.ParentId == null && item.TypeId == null && !item.HoldsItems && item.SuggestedTypeId == null)
                .Where(item => item.TypeSuggestedAt == null || (before != null && item.TypeSuggestedAt < before))
                .OrderBy(item => item.Id)
                .Take(limit)
                .ToListAsync(cancellationToken))
            .Select(item => item.ToContract())
            .ToList();
    }

    /// <summary>One UPDATE per suggested type, opening the request's transaction first as <see cref="MoveContentsAsync"/> does.</summary>
    public async Task SuggestTypesAsync(Guid spaceId, IReadOnlyDictionary<Guid, Guid?> suggestions, DateTimeOffset at, CancellationToken cancellationToken)
    {
        if (suggestions.Count == 0)
        {
            return;
        }

        if (db.Database.CurrentTransaction is null)
        {
            await db.Database.BeginTransactionAsync(cancellationToken);
        }

        var asked = at.ToUniversalTime();
        foreach (var group in suggestions.GroupBy(pair => pair.Value, pair => pair.Key))
        {
            var ids = group.ToList();
            var typeId = group.Key;
            await db.Items
                .Where(item => item.SpaceId == spaceId && ids.Contains(item.Id))
                .ExecuteUpdateAsync(
                    set => set.SetProperty(item => item.SuggestedTypeId, typeId).SetProperty(item => item.TypeSuggestedAt, asked),
                    cancellationToken);
            foreach (var tracked in db.Items.Local.Where(item => item.SpaceId == spaceId && ids.Contains(item.Id)))
            {
                // Keep tracked rows in step, so a later save in this request doesn't put the old values back.
                var entry = db.Entry(tracked);
                entry.Property(item => item.SuggestedTypeId).CurrentValue = typeId;
                entry.Property(item => item.SuggestedTypeId).OriginalValue = typeId;
                entry.Property(item => item.TypeSuggestedAt).CurrentValue = asked;
                entry.Property(item => item.TypeSuggestedAt).OriginalValue = asked;
            }
        }
    }
}
