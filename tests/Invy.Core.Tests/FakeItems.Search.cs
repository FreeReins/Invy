using System.Text.Json;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

/// <content>
/// Search and rollups in memory. Words match by contains rather than by PostgreSQL's prefixes and trigrams, which only
/// a dev run against the database checks; everything else follows SearchSql.
/// </content>
internal sealed partial class FakeItems
{
    public Task<List<SearchHit>> SearchAsync(Guid spaceId, ItemQuery query, SearchHit? after, int limit, CancellationToken cancellationToken)
    {
        var hits = Matching(spaceId, query)
            .Select(item => new SearchHit(item.Id, Rank(item, query), item.Name, item.UpdatedAt));
        hits = query.Sort switch
        {
            SearchOrder.Relevance => hits.OrderByDescending(hit => hit.Rank).ThenBy(hit => hit.Id)
                .Where(hit => after is null || hit.Rank < after.Rank || (hit.Rank == after.Rank && hit.Id.CompareTo(after.Id) > 0)),
            SearchOrder.Recent => hits.OrderByDescending(hit => hit.UpdatedAt).ThenByDescending(hit => hit.Id)
                .Where(hit => after is null || hit.UpdatedAt < after.UpdatedAt || (hit.UpdatedAt == after.UpdatedAt && hit.Id.CompareTo(after.Id) < 0)),
            _ => hits.OrderBy(hit => hit.Name, StringComparer.Ordinal).ThenBy(hit => hit.Id)
                .Where(hit => after is null || string.CompareOrdinal(hit.Name, after.Name) > 0 || (hit.Name == after.Name && hit.Id.CompareTo(after.Id) > 0)),
        };
        return Task.FromResult(hits.Take(limit).ToList());
    }

    public Task<SearchCount> CountAsync(Guid spaceId, ItemQuery query, int cap, CancellationToken cancellationToken)
    {
        var count = Matching(spaceId, query).Count();
        return Task.FromResult(new SearchCount(count, count > cap));
    }

    public Task RecountAsync(Guid spaceId, IReadOnlyCollection<Guid> containerIds, CancellationToken cancellationToken)
    {
        foreach (var container in Rows.Where(item => item.SpaceId == spaceId && containerIds.Contains(item.Id)))
        {
            var inside = Current(spaceId).Where(item => item.Id != container.Id && ItemPaths.IsWithin(item.Path, container.Path)).ToList();
            container.ContentCount = inside.Count;
            container.ContentValue = inside.Sum(item => item.Value ?? 0);
        }

        return Task.CompletedTask;
    }

    private IEnumerable<Item> Matching(Guid spaceId, ItemQuery query) =>
        Rows.Where(item => item.SpaceId == spaceId
            && (query.IncludeGone || item.Status is not (ItemStatus.Disposed or ItemStatus.Archived))
            && Holds(query.Filter, item));

    private static float Rank(Item item, ItemQuery query) =>
        query.Words.Count(word => item.Name.Contains(word, StringComparison.OrdinalIgnoreCase));

    private bool Holds(QueryNode node, Item item) => node switch
    {
        AllOf all => all.Parts.All(part => Holds(part, item)),
        AnyOf any => any.Parts.Any(part => Holds(part, item)),
        Not not => !Holds(not.Part, item),
        Nothing => false,
        Words words => Texts(item).Any(text => text.Contains(words.Text, StringComparison.OrdinalIgnoreCase)),
        OfType type => item.TypeId is Guid typeId && type.TypeIds.Contains(typeId),
        Inside inside => Rows.Any(container => container.SpaceId == item.SpaceId && container.HoldsItems && container.Id != item.Id
            && ItemPaths.IsWithin(item.Path, container.Path)
            && (string.Equals(container.Name, inside.Container, StringComparison.OrdinalIgnoreCase)
                || string.Equals(container.Marking, inside.Container, StringComparison.OrdinalIgnoreCase)
                || string.Equals(container.LabelCode, inside.Container, StringComparison.OrdinalIgnoreCase))),
        Contains contains => (contains.Column switch { ItemText.Name => item.Name, ItemText.Marking => item.Marking, _ => item.Notes })
            ?.Contains(contains.Text, StringComparison.OrdinalIgnoreCase) == true,
        Tagged tag => item.Tags.Contains(tag.Tag),
        FieldIs field => field.Matches.Any(match => item.Fields.TryGetValue(match.FieldId, out var value) && Matches(value, match)),
        FieldRange range => Values.Any(value => value.ItemId == item.Id && range.FieldIds.Contains(value.FieldId)
            && (range.Number is { } number ? In(value.Num, number) : In(value.Date, range.Date!))),
        QuantityIn quantity => In(item.Quantity, quantity.Range),
        SeenIn seen => (seen.OrNever && item.LastSeenAt is null) || In(item.LastSeenAt, seen.Range),
        AddedIn added => In(item.CreatedAt, added.Range),
        ReservedFor reserved => ReservedFor(item.Id, reserved.Project),
        LentTo lent => item.LentTo?.Contains(lent.Text, StringComparison.OrdinalIgnoreCase) == true,
        LoanDue due => item.LentAt is not null && In(item.DueOn, due.Range),
        GoingTo going => Rows.Any(place => place.SpaceId == item.SpaceId && place.Id == item.DestinationId
            && (string.Equals(place.Name, going.Place, StringComparison.OrdinalIgnoreCase)
                || string.Equals(place.Marking, going.Place, StringComparison.OrdinalIgnoreCase)
                || string.Equals(place.LabelCode, going.Place, StringComparison.OrdinalIgnoreCase))),
        Flag flag => flag.Kind switch
        {
            ItemFlag.Low => item.IsLow,
            ItemFlag.Container => item.HoldsItems,
            ItemFlag.Counted => item.Quantity is not null,
            ItemFlag.Unique => item.Quantity is null,
            ItemFlag.HasBarcode => flag.FieldIds.Any(item.Fields.ContainsKey),
            _ => item.Status.ToString() == flag.Kind.ToString(),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private static IEnumerable<string> Texts(Item item) =>
        new[] { item.Name, item.Marking, item.LabelCode, item.Notes }.OfType<string>().Concat(item.Tags)
            .Concat(item.Fields.Values.Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!));

    private static bool Matches(JsonElement value, FieldMatch match) =>
        match.Json is JsonElement json
            ? json.ValueKind == JsonValueKind.Array
                ? value.ValueKind == JsonValueKind.Array && json.EnumerateArray().All(wanted => value.EnumerateArray().Any(have => JsonElement.DeepEquals(have, wanted)))
                : json.ValueKind == JsonValueKind.Number ? value.ValueKind == JsonValueKind.Number && value.GetDecimal() == json.GetDecimal() : JsonElement.DeepEquals(value, json)
            : value.ValueKind == JsonValueKind.String && value.GetString()!.ToLowerInvariant() == match.LowerText;

    private static bool In<T>(T? value, Range<T> range)
        where T : struct, IComparable<T> =>
        value is T have
        && (range.Low is not T low || (range.LowInclusive ? have.CompareTo(low) >= 0 : have.CompareTo(low) > 0))
        && (range.High is not T high || (range.HighInclusive ? have.CompareTo(high) <= 0 : have.CompareTo(high) < 0));
}
