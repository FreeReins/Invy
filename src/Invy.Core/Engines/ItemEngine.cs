using System.Globalization;
using System.Text.Json;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>Works out what follows from an item's values: its sortable copies, and what changed between two versions.</summary>
internal sealed class ItemEngine
{
    /// <summary>One row per sortable field of the type that has a value, typed for ranges and sorting.</summary>
    public IReadOnlyList<ItemValue> SortableValues(Item item, ItemType? type)
    {
        var values = new List<ItemValue>();
        foreach (var field in type?.Fields.Where(field => field.Sortable) ?? [])
        {
            if (!item.Fields.TryGetValue(field.Id, out var value))
            {
                continue;
            }

            var row = new ItemValue { SpaceId = item.SpaceId, ItemId = item.Id, FieldId = field.Id };
            switch (field.Kind)
            {
                case FieldKind.Number or FieldKind.Money when value.ValueKind == JsonValueKind.Number:
                    row.Num = value.GetDecimal();
                    break;
                case FieldKind.Bool when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                    row.Num = value.GetBoolean() ? 1 : 0;
                    break;
                case FieldKind.Date when value.ValueKind == JsonValueKind.String
                    && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date):
                    row.Date = date;
                    break;
                case not (FieldKind.Number or FieldKind.Money or FieldKind.Bool or FieldKind.Date) when value.ValueKind == JsonValueKind.String:
                    row.Text = value.GetString()!.ToLowerInvariant();
                    break;
                default:
                    continue;
            }

            values.Add(row);
        }

        return values;
    }

    /// <summary>
    /// The item's value: its type's first money field that has a value, times its amount (1 for a unique item).
    /// Null when the type has no money field or the item has no value in it.
    /// </summary>
    public decimal? Value(Item item, ItemType? type)
    {
        foreach (var field in type?.Fields.Where(field => field.Kind == FieldKind.Money) ?? [])
        {
            if (item.Fields.TryGetValue(field.Id, out var value) && value.ValueKind == JsonValueKind.Number)
            {
                return decimal.Round(value.GetDecimal() * (item.Quantity ?? 1), 2);
            }

            return null;
        }

        return null;
    }

    /// <summary>
    /// What changed from <paramref name="before"/> to <paramref name="after"/>, for a Changed event: the plain details,
    /// then each field, named by its label in the type it has now (or had). Amounts and moves have events of their own.
    /// </summary>
    public IReadOnlyList<ItemChange> Changes(Item before, Item after, ItemType? typeBefore, ItemType? typeAfter)
    {
        var changes = new List<ItemChange>();
        void Compare(string what, string? old, string? now)
        {
            if (old != now)
            {
                changes.Add(new ItemChange(what, old, now));
            }
        }

        Compare("Name", before.Name, after.Name);
        Compare("Marking", before.Marking, after.Marking);
        Compare("Type", typeBefore?.Name, typeAfter?.Name);
        Compare("Holds items", before.HoldsItems ? "Yes" : "No", after.HoldsItems ? "Yes" : "No");
        Compare("Unit", before.Unit, after.Unit);
        Compare("Tags", Join(before.Tags), Join(after.Tags));
        Compare("Notes", before.Notes, after.Notes);

        var labels = (typeBefore?.Fields ?? []).Concat(typeAfter?.Fields ?? [])
            .GroupBy(field => field.Id).ToDictionary(group => group.Key, group => group.Last().Label);
        foreach (var id in before.Fields.Keys.Union(after.Fields.Keys))
        {
            var old = before.Fields.TryGetValue(id, out var oldValue) ? Display(oldValue) : null;
            var now = after.Fields.TryGetValue(id, out var newValue) ? Display(newValue) : null;
            Compare(labels.GetValueOrDefault(id) ?? "Other field", old, now);
        }

        return changes;
    }

    /// <summary>A field value as plain text: lists joined by commas, yes or no for true and false.</summary>
    public static string? Display(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetDecimal().ToString(CultureInfo.InvariantCulture),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Array => Join(value.EnumerateArray().Select(Display).OfType<string>()),
        _ => null,
    };

    private static string? Join(IEnumerable<string> values) => string.Join(", ", values) is { Length: > 0 } joined ? joined : null;

    /// <summary>Sets the item's container, path and depth for being inside <paramref name="parent"/> (null for the top).</summary>
    public static void Place(Item item, Item? parent)
    {
        item.ParentId = parent?.Id;
        item.Path = ItemPaths.Of(item.Id, parent?.Path);
        item.Depth = ItemPaths.Depth(item.Path);
    }

    /// <summary>A new timeline event for the item.</summary>
    public static ItemEvent Event(Item item, Guid userId, ItemEventKind kind, DateTimeOffset at, Guid? from = null, Guid? to = null) => new()
    {
        SpaceId = item.SpaceId,
        ItemId = item.Id,
        At = at,
        ActorId = userId,
        Kind = kind,
        FromParent = from,
        ToParent = to,
    };
}
