using System.Globalization;
using System.Buffers.Text;
using System.Text.Json;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <content>Checks on items: their details, field values, containers and moves.</content>
internal sealed partial class ValidationEngine
{
    public const int ItemNameLength = 200;

    public const int MarkingLength = 40;

    public const int NotesLength = 10_000;

    public const int TagLength = 40;

    public const int MaxTags = 50;

    public const int TextValueLength = 2_000;

    public const int LongTextValueLength = 10_000;

    /// <summary>The largest amount a counted item can hold.</summary>
    public const decimal MaxQuantity = 1_000_000_000m;

    public async Task<Item> ItemAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        await items.FindAsync(spaceId, id, cancellationToken)
        ?? throw new NotFoundException("That item no longer exists. It may have been removed; go back and reload.");

    /// <summary>The container items are going into, or null for the top.</summary>
    public async Task<Item?> ContainerAsync(Guid spaceId, Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is not Guid id)
        {
            return null;
        }

        var parent = await items.FindAsync(spaceId, id, cancellationToken)
            ?? throw new NotFoundException("That container no longer exists. Pick another place.");
        if (!parent.HoldsItems)
        {
            throw new RuleViolationException($"{parent.Name} doesn't hold items. Turn on Holds items for it first, or pick another place.");
        }

        if (parent.Status is ItemStatus.Disposed or ItemStatus.Archived)
        {
            throw new RuleViolationException($"{parent.Name} is {parent.Status.ToString().ToLowerInvariant()}. Restore it first, or pick another place.");
        }

        return parent;
    }

    /// <summary>The item's type, or null for none.</summary>
    public async Task<ItemType?> ItemTypeAsync(Guid spaceId, Guid? typeId, CancellationToken cancellationToken) =>
        typeId is Guid id ? await TypeAsync(spaceId, id, cancellationToken) : null;

    /// <summary>Checks that <paramref name="item"/> can go into <paramref name="target"/>: not into itself or anything inside it.</summary>
    public static void MoveTarget(Item item, Item? target)
    {
        if (target is not null && ItemPaths.IsWithin(target.Path, item.Path))
        {
            throw new RuleViolationException(target.Id == item.Id
                ? $"{item.Name} can't go inside itself. Pick another place."
                : $"{target.Name} is inside {item.Name}, so {item.Name} can't go into it. Move {target.Name} out first.");
        }
    }

    /// <summary>Checks that nothing current is inside an item, before doing <paramref name="action"/> (such as "dispose of it").</summary>
    public async Task EmptyAsync(Item item, string action, CancellationToken cancellationToken)
    {
        var count = (await items.CountChildrenAsync(item.SpaceId, [item.Id], cancellationToken)).GetValueOrDefault(item.Id);
        if (count > 0)
        {
            throw new RuleViolationException(
                $"{item.Name} still has {(count == 1 ? "1 item" : $"{count} items")} inside. Move them out first, then {action}.");
        }
    }

    /// <summary>Reads a page cursor this API handed out (see Mapping.Cursor); null for the first page.</summary>
    public static ItemCursor? ItemCursor(string? after)
    {
        if (string.IsNullOrEmpty(after))
        {
            return null;
        }

        try
        {
            var text = System.Text.Encoding.UTF8.GetString(Base64Url.DecodeFromChars(after));
            return new ItemCursor(text[32..], Guid.ParseExact(text[..32], "N"));
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            throw new RuleViolationException("That page link is damaged. Go back to the start of the list.");
        }
    }

    /// <summary>Reads a search page cursor (see Mapping.Cursor); null for the first page.</summary>
    public static SearchHit? SearchCursor(string? after)
    {
        if (string.IsNullOrEmpty(after))
        {
            return null;
        }

        try
        {
            var parts = System.Text.Encoding.UTF8.GetString(Base64Url.DecodeFromChars(after)).Split('|', 4);
            return new SearchHit(
                Guid.ParseExact(parts[2], "N"),
                float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                parts[3],
                new DateTimeOffset(long.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture), TimeSpan.Zero));
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or IndexOutOfRangeException or OverflowException)
        {
            throw new RuleViolationException("That page link is damaged. Search again.");
        }
    }

    /// <summary>Reads an activity page cursor (see Mapping.Cursor); null for the newest page.</summary>
    public static EventCursor? EventCursor(string? before)
    {
        if (string.IsNullOrEmpty(before))
        {
            return null;
        }

        var parts = before.Split('.');
        return parts.Length == 2 && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            && ticks is >= 0 and <= 3_155_378_975_999_999_999 && Guid.TryParseExact(parts[1], "N", out var id)
            ? new EventCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id)
            : throw new RuleViolationException("That page link is damaged. Go back to the newest activity.");
    }

    /// <summary>Copies the request's plain details onto <paramref name="item"/>, checked and trimmed.</summary>
    public static void ItemDetails(Item item, ItemRequest request, ItemType? type)
    {
        var name = Trimmed(request.Name);
        item.Name = name.Length is > 0 and <= ItemNameLength
            ? name
            : throw new RuleViolationException($"An item needs a name of 1 to {ItemNameLength} characters.");

        var marking = Optional(request.Marking);
        item.Marking = marking is null || marking.Length <= MarkingLength
            ? marking
            : throw new RuleViolationException($"A marking can have up to {MarkingLength} characters.");

        item.TypeId = type?.Id;
        item.HoldsItems = request.HoldsItems ?? type?.HoldsItems ?? false;

        var notes = Optional(request.Notes);
        item.Notes = notes is null || notes.Length <= NotesLength
            ? notes
            : throw new RuleViolationException($"Notes can have up to {NotesLength:N0} characters.");

        // Tags are kept in lower case, so tag:Garden finds garden.
        var tags = (request.Tags ?? []).Select(tag => Trimmed(tag).ToLowerInvariant()).Where(tag => tag.Length > 0).Distinct().ToList();
        if (tags.Count > MaxTags || tags.Any(tag => tag.Length > TagLength))
        {
            throw new RuleViolationException($"An item can have up to {MaxTags} tags of up to {TagLength} characters each.");
        }

        item.Tags = tags;
        item.Quantity = Quantity(request.Quantity);
        var unit = Optional(request.Unit);
        if (unit is not null && item.Quantity is null)
        {
            throw new RuleViolationException("Only counted items have a unit. Give it an amount, or clear the unit.");
        }

        item.Unit = unit is null || unit.Length <= UnitLength
            ? unit
            : throw new RuleViolationException($"A unit can have up to {UnitLength} characters.");

        item.LowStockAt = request.LowStockAt is null || item.Quantity is not null
            ? Quantity(request.LowStockAt)
            : throw new RuleViolationException("Only counted items can run low. Give it an amount, or clear Low stock at.");
    }

    /// <summary>The longest name of who or where something is lent to.</summary>
    public const int BorrowerLength = 100;

    /// <summary>Who or where something is lent to, trimmed.</summary>
    public static string Borrower(string? to)
    {
        var borrower = Optional(to) ?? throw new RuleViolationException("Say who or where it's lent to, such as Sam or the church hall.");
        return borrower.Length <= BorrowerLength
            ? borrower
            : throw new RuleViolationException($"Who it's lent to can have up to {BorrowerLength} characters.");
    }

    public static decimal? Quantity(decimal? quantity) => quantity switch
    {
        null => null,
        < 0 => throw new RuleViolationException("An amount can't be below zero."),
        > MaxQuantity => throw new RuleViolationException($"An amount can be at most {MaxQuantity:N0}."),
        _ => decimal.Round(quantity.Value, 3),
    };

    /// <summary>
    /// The field values to store: each checked against its field's kind. Values for fields the type doesn't have
    /// (another type's, or a removed field's) are kept only when sent back unchanged.
    /// </summary>
    public async Task<Dictionary<Guid, JsonElement>> FieldValuesAsync(
        Guid spaceId, ItemType? type, IReadOnlyDictionary<Guid, JsonElement>? requested, IReadOnlyDictionary<Guid, JsonElement> stored,
        CancellationToken cancellationToken)
    {
        var fields = type?.Fields.ToDictionary(field => field.Id) ?? [];
        var values = new Dictionary<Guid, JsonElement>();
        foreach (var (id, value) in requested ?? new Dictionary<Guid, JsonElement>())
        {
            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            if (fields.TryGetValue(id, out var field))
            {
                values[id] = await FieldValueAsync(spaceId, field, value, cancellationToken);
            }
            else if (stored.TryGetValue(id, out var kept) && JsonElement.DeepEquals(kept, value))
            {
                values[id] = kept.Clone();
            }
            else
            {
                throw new RuleViolationException("A value was sent for a field this type doesn't have. Reload the item and try again.");
            }
        }

        return values;
    }

    private async Task<JsonElement> FieldValueAsync(Guid spaceId, TypeField field, JsonElement value, CancellationToken cancellationToken)
    {
        var wrong = new RuleViolationException(field.Kind switch
        {
            FieldKind.Number or FieldKind.Money => $"{field.Label} needs a number.",
            FieldKind.Date => $"{field.Label} needs a date.",
            FieldKind.Bool => $"{field.Label} needs yes or no.",
            FieldKind.Select => $"{field.Label} needs one of its choices: {string.Join(", ", field.Options)}.",
            FieldKind.MultiSelect => $"{field.Label} takes only its choices: {string.Join(", ", field.Options)}.",
            FieldKind.Url => $"{field.Label} needs a web address starting with http:// or https://.",
            FieldKind.Item => $"{field.Label} needs another item in this space.",
            FieldKind.LongText => $"{field.Label} can have up to {LongTextValueLength:N0} characters.",
            _ => $"{field.Label} needs text of up to {TextValueLength:N0} characters.",
        });

        switch (field.Kind)
        {
            case FieldKind.Number or FieldKind.Money:
                return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out _) ? value.Clone() : throw wrong;
            case FieldKind.Bool:
                return value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.Clone() : throw wrong;
            case FieldKind.Date:
                return value.ValueKind == JsonValueKind.String
                    && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    ? value.Clone()
                    : throw wrong;
            case FieldKind.Select:
                return Text(value) is { } choice && field.Options.Contains(choice) ? value.Clone() : throw wrong;
            case FieldKind.MultiSelect:
                if (value.ValueKind != JsonValueKind.Array)
                {
                    throw wrong;
                }

                var chosen = value.EnumerateArray().Select(Text).ToList();
                return chosen.All(option => option is not null && field.Options.Contains(option)) && chosen.Distinct().Count() == chosen.Count
                    ? value.Clone()
                    : throw wrong;
            case FieldKind.Url:
                return Text(value) is { } url && url.Length <= TextValueLength
                    && Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    ? value.Clone()
                    : throw wrong;
            case FieldKind.Item:
                return Text(value) is { } text && Guid.TryParse(text, out var itemId)
                    && await items.FindAsync(spaceId, itemId, cancellationToken) is not null
                    ? value.Clone()
                    : throw wrong;
            default:
                var limit = field.Kind == FieldKind.LongText ? LongTextValueLength : TextValueLength;
                return Text(value) is { Length: > 0 } words && words.Length <= limit ? value.Clone() : throw wrong;
        }
    }

    private static string? Text(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
