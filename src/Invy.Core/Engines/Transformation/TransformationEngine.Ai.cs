using System.Text.Json;
using System.Text.Json.Nodes;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Engines.Transformation;

/// <content>Questions for AI models, and reading their answers. A bad answer reads as an empty suggestion, never an error.</content>
internal sealed partial class TransformationEngine
{
    /// <summary>The value <paramref name="text"/> stands for in <paramref name="field"/>'s kind, read as leniently as a model's answer; null when it doesn't fit.</summary>
    public static System.Text.Json.JsonElement? FieldValue(TypeField field, string text) => FieldValues.Value(field, text.Trim());

    /// <summary>The most things read from one photo answer.</summary>
    public const int MaxSuggestedItems = 50;

    private const int NameLength = 200;

    private const int UnitLength = 20;

    private const decimal MaxQuantity = 1_000_000_000m;

    /// <summary>A short question that only checks a model answers, and answers in JSON.</summary>
    public static AiQuestion AiTestQuestion() => new(
        "You are checking that a connection works. Answer only with the JSON asked for.",
        "Set ok to true.",
        [],
        """{"type":"object","properties":{"ok":{"type":"boolean"}},"required":["ok"],"additionalProperties":false}""",
        "connection_check");

    /// <summary>The question that lists the things in photos of an open box, with the space's types.</summary>
    public static AiQuestion ItemsFromPhotoQuestion(IReadOnlyList<AiImage> photos, IReadOnlyList<ItemType> types) => new(
        ItemsFromPhotoPrompt.Instructions,
        ItemsFromPhotoPrompt.Question(TypeCatalog.Describe(types)),
        photos,
        ItemsFromPhotoPrompt.Schema(types),
        ItemsFromPhotoPrompt.SchemaName);

    /// <summary>
    /// The things a model saw. Each needs a name; a type id that isn't one of <paramref name="types"/> is dropped, and
    /// so are field values that aren't the chosen type's or don't fit their kind.
    /// </summary>
    public static List<ItemSuggestion> ReadItemSuggestions(string json, IReadOnlyList<ItemType> types)
    {
        if (AiAnswer.Parse(json) is not { } answer || !answer.TryGetProperty("items", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var byId = types.ToDictionary(type => type.Id);
        var suggestions = new List<ItemSuggestion>();
        foreach (var entry in list.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).Take(MaxSuggestedItems))
        {
            if (AiAnswer.Text(entry, "name") is not { } name)
            {
                continue;
            }

            var type = Guid.TryParse(AiAnswer.Text(entry, "typeId"), out var typeId) ? byId.GetValueOrDefault(typeId) : null;
            var unit = AiAnswer.Text(entry, "unit") is { Length: <= UnitLength } written ? written : null;
            var amount = AiAnswer.Number(entry, "quantity") is { } number && number <= MaxQuantity ? number : (decimal?)null;
            var counted = amount is not null && (amount != 1 || unit is not null);
            suggestions.Add(new ItemSuggestion(
                name.Length > NameLength ? name[..NameLength].TrimEnd() : name,
                type?.Id,
                counted ? amount : null,
                counted ? unit : null,
                type is null ? new Dictionary<Guid, JsonElement>() : FieldValues.Read(entry, "fields", type.Fields)));
        }

        return suggestions;
    }

    /// <summary>The question that reads one item's fields from photos of a label or nameplate.</summary>
    public static AiQuestion ItemDetailsQuestion(IReadOnlyList<AiImage> photos, string itemName, ItemType type) => new(
        ItemDetailsFromPhotoPrompt.Instructions,
        ItemDetailsFromPhotoPrompt.Question(itemName, TypeCatalog.Describe([type])),
        photos,
        ItemDetailsFromPhotoPrompt.Schema(type),
        ItemDetailsFromPhotoPrompt.SchemaName);

    /// <summary>The field values a model read for an item of <paramref name="type"/>; empty when it found no label.</summary>
    public static Dictionary<Guid, JsonElement> ReadItemDetails(string json, ItemType type) =>
        AiAnswer.Parse(json) is { } answer && !(answer.TryGetProperty("found", out var found) && found.ValueKind == JsonValueKind.False)
            ? FieldValues.Read(answer, "fields", type.Fields)
            : [];

    /// <summary>The question that suggests fields for a type called <paramref name="name"/>.</summary>
    public static AiQuestion TypeFieldsQuestion(string name, IReadOnlyList<string> existingKeys) => new(
        SuggestTypeFieldsPrompt.Instructions,
        SuggestTypeFieldsPrompt.Question(name, existingKeys),
        [],
        SuggestTypeFieldsPrompt.Schema,
        SuggestTypeFieldsPrompt.SchemaName);

    /// <summary>
    /// The fields a model suggested, each with a search name made from its label. Fields whose search name is taken
    /// (by <paramref name="takenKeys"/> or an earlier suggestion) or can't be made are dropped, and so are units and
    /// options on kinds that don't have them. The caller still checks each one as the Types editor would.
    /// </summary>
    public static List<FieldDefinition> ReadTypeFields(string json, IEnumerable<string> takenKeys)
    {
        if (AiAnswer.Parse(json) is not { } answer || !answer.TryGetProperty("fields", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var taken = new HashSet<string>(takenKeys, StringComparer.Ordinal);
        var fields = new List<FieldDefinition>();
        foreach (var entry in list.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object))
        {
            if (fields.Count == SuggestTypeFieldsPrompt.MaxFields)
            {
                break;
            }

            if (AiAnswer.Text(entry, "label") is not { } label
                || !Enum.TryParse<FieldKind>(AiAnswer.Text(entry, "kind"), true, out var kind)
                || !SuggestTypeFieldsPrompt.Kinds.Contains(kind)
                || FieldKeys.FromLabel(label) is not { } key
                || !taken.Add(key))
            {
                continue;
            }

            var choices = kind is FieldKind.Select or FieldKind.MultiSelect
                ? AiAnswer.Lines(entry, "options").Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : [];
            if (kind is FieldKind.Select or FieldKind.MultiSelect && choices.Count == 0)
            {
                kind = FieldKind.Text;
            }

            var unit = kind is FieldKind.Number or FieldKind.Money ? AiAnswer.Text(entry, "unit") : null;
            fields.Add(new FieldDefinition(null, key, label, kind, unit, choices, false, AiAnswer.Flag(entry, "sortable")));
        }

        return fields;
    }

    /// <summary>The question that suggests a type for each of these Inbox items.</summary>
    public static AiQuestion InboxTypesQuestion(IReadOnlyList<Item> items, IReadOnlyList<ItemType> types)
    {
        var described = new JsonArray();
        foreach (var item in items)
        {
            var entry = new JsonObject { ["id"] = item.Id.ToString(), ["name"] = item.Name };
            if (item.Marking is { } marking)
            {
                entry["marking"] = marking;
            }

            if (item.Tags.Count > 0)
            {
                entry["tags"] = new JsonArray([.. item.Tags.Select(tag => (JsonNode)tag)]);
            }

            if (item.Notes is { } notes)
            {
                entry["notes"] = notes.Length > 300 ? notes[..300] : notes;
            }

            described.Add(entry);
        }

        return new(
            SuggestTypePrompt.Instructions,
            SuggestTypePrompt.Question(described.ToJsonString(), TypeCatalog.Describe(types)),
            [],
            SuggestTypePrompt.Schema(items, types),
            SuggestTypePrompt.SchemaName);
    }

    /// <summary>
    /// The type suggested for each item asked about: every one of <paramref name="items"/> is in the answer, with null
    /// when the model gave none, gave an id that isn't one of <paramref name="types"/>, or left the item out.
    /// </summary>
    public static Dictionary<Guid, Guid?> ReadInboxTypes(string json, IReadOnlyList<Item> items, IReadOnlyList<ItemType> types)
    {
        var suggested = items.ToDictionary(item => item.Id, _ => (Guid?)null);
        var known = types.Select(type => type.Id).ToHashSet();
        if (AiAnswer.Parse(json) is not { } answer || !answer.TryGetProperty("suggestions", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return suggested;
        }

        foreach (var entry in list.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object))
        {
            if (Guid.TryParse(AiAnswer.Text(entry, "itemId"), out var itemId)
                && suggested.TryGetValue(itemId, out var already) && already is null
                && Guid.TryParse(AiAnswer.Text(entry, "typeId"), out var typeId) && known.Contains(typeId))
            {
                suggested[itemId] = typeId;
            }
        }

        return suggested;
    }
}
