using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

/// <content>Examples and what a space has so far, as <c>ItemAccess.Examples</c> works them out.</content>
internal sealed partial class FakeItems
{
    /// <summary>How many times examples were removed in one statement.</summary>
    public int ExampleRemovals { get; private set; }

    public Task<SpaceProgress> ProgressAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var mine = Current(spaceId).Where(item => !item.IsExample).ToList();
        return Task.FromResult(new SpaceProgress
        {
            HasPlaces = mine.Any(item => item.ParentId is null && item.HoldsItems),
            HasPutAway = mine.Any(item => item.ParentId is not null),
            HasValues = mine.Any(item => item.Value is not null),
            HasSeen = mine.Any(item => item.LastSeenAt is not null),
            HasLowStockLevels = mine.Any(item => item.LowStockAt is not null),
            HasLent = Events.Any(e => e.SpaceId == spaceId && e.Kind == ItemEventKind.Lent),
            HasDestinations = mine.Any(item => item.DestinationId is not null),
            ExamplesLeft = Rows.Count(item => item.SpaceId == spaceId && item.IsExample),
        });
    }

    public Task<List<string>> RemoveExamplesAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var inSpace = Rows.Where(item => item.SpaceId == spaceId).ToList();
        var removed = inSpace
            .Where(example => example.IsExample)
            .Where(example => !inSpace.Any(other => !other.IsExample && ItemPaths.IsWithin(other.Path, example.Path)))
            .ToList();
        var ids = removed.Select(item => item.Id).ToHashSet();
        Rows.RemoveAll(item => ids.Contains(item.Id));
        Events.RemoveAll(itemEvent => ids.Contains(itemEvent.ItemId));
        Values.RemoveAll(value => ids.Contains(value.ItemId));
        ExampleRemovals++;
        return Task.FromResult(removed.Select(item => item.Path).ToList());
    }
}
