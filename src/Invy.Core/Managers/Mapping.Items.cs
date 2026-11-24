using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Items, their events and page cursors.</content>
internal static partial class Mapping
{
    public static ItemResponse ToResponse(this Item item) => new(
        item.Id, item.Name, item.Marking, item.TypeId, item.ParentId, item.HoldsItems, item.Quantity, item.Unit,
        item.Fields, item.Tags, item.Notes, item.Status, item.LastSeenAt, item.CreatedAt, item.UpdatedAt, item.Version,
        item.Value, item.ContentCount, item.ContentValue, item.LabelCode, item.SuggestedTypeId);

    public static ItemSummary ToSummary(this Item item) =>
        new(item.Id, item.Name, item.Marking, item.TypeId, item.HoldsItems, item.Quantity, item.Unit, item.Status, item.ContentCount, item.Value, item.ContentValue, item.LabelCode, item.SuggestedTypeId);

    public static ItemDetails ToDetails(this Item item, Guid? photoId) =>
        new(item.Fields, item.Tags, item.LastSeenAt, item.CreatedAt, photoId);

    public static ItemRef ToRef(this Item item) => new(item.Id, item.Name, item.Marking);

    public static ItemEventResponse ToResponse(
        this ItemEvent itemEvent, IReadOnlyDictionary<Guid, ItemRef> refs, IReadOnlyDictionary<Guid, string> actors) => new(
        itemEvent.Id,
        itemEvent.Kind,
        itemEvent.At,
        refs[itemEvent.ItemId],
        itemEvent.ActorId is Guid actor ? actors.GetValueOrDefault(actor) ?? "A removed account" : null,
        itemEvent.FromParent is Guid from ? refs[from] : null,
        itemEvent.ToParent is Guid to ? refs[to] : null,
        itemEvent.QtyDelta,
        itemEvent.Changes);

    /// <summary>A copy to change, keeping the original to compare with.</summary>
    public static Item Copy(this Item item) => new()
    {
        Id = item.Id,
        SpaceId = item.SpaceId,
        TypeId = item.TypeId,
        Name = item.Name,
        Marking = item.Marking,
        LabelCode = item.LabelCode,
        ParentId = item.ParentId,
        Path = item.Path,
        Depth = item.Depth,
        HoldsItems = item.HoldsItems,
        Quantity = item.Quantity,
        Unit = item.Unit,
        Fields = new(item.Fields),
        Tags = [.. item.Tags],
        Notes = item.Notes,
        Status = item.Status,
        LastSeenAt = item.LastSeenAt,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        Value = item.Value,
        ContentCount = item.ContentCount,
        ContentValue = item.ContentValue,
        SuggestedTypeId = item.SuggestedTypeId,
        TypeSuggestedAt = item.TypeSuggestedAt,
        IsExample = item.IsExample,
        Version = item.Version,
    };

    /// <summary>An item's amount as text, such as "3.5 kg", or null for a unique item.</summary>
    public static string? Amount(Item item) =>
        item.Quantity is decimal amount
            ? $"{amount.ToString("0.###", CultureInfo.InvariantCulture)}{(item.Unit is null ? string.Empty : $" {item.Unit}")}"
            : null;

    /// <summary>The cursor for the page after <paramref name="last"/>: its id then its name, base64url.</summary>
    public static string Cursor(Item last) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(last.Id.ToString("N") + last.Name));

    /// <summary>The cursor for the search results after <paramref name="last"/>: what each order sorts by, then the name, base64url.</summary>
    public static string Cursor(SearchHit last) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(string.Join(
            '|',
            last.Rank.ToString("R", CultureInfo.InvariantCulture),
            last.UpdatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
            last.Id.ToString("N"),
            last.Name)));

    /// <summary>The cursor for the events older than <paramref name="last"/>: its time in ticks and its id.</summary>
    public static string Cursor(ItemEvent last) =>
        $"{last.At.UtcTicks.ToString(CultureInfo.InvariantCulture)}.{last.Id:N}";
}
