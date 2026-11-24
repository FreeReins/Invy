using System.Text.Json;
using Invy.Core.Engines.Transformation;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Calls shaped for assistants and scripts: where things are, and changing only part of an item.</content>
internal sealed partial class ItemManager
{
    /// <summary>How many matches <see cref="WhereIsAsync"/> gives.</summary>
    public const int WhereIsMatches = 10;

    public async Task<WhereIsResponse> WhereIsAsync(Guid userId, Guid spaceId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new RuleViolationException("Say what to look for, such as baseball.");
        }

        var found = await SearchAsync(userId, spaceId, text, null, null, cancellationToken);
        var matches = found.Results.Take(WhereIsMatches).Select(result => new ItemLocation(
            result.Item.Id,
            result.Item.Marking is { } marking && !result.Item.Name.Contains(marking, StringComparison.Ordinal) ? $"{result.Item.Name} ({marking})" : result.Item.Name,
            Where(result),
            result.Item.Status,
            result.Item.Quantity is null ? null : Mapping.Amount(new Item { Quantity = result.Item.Quantity, Unit = result.Item.Unit }),
            null)).ToList();

        // Last seen isn't in a search summary; read it for the few matches shown.
        var seen = (await items.FindManyAsync(spaceId, matches.Select(match => match.Id).ToList(), cancellationToken))
            .ToDictionary(item => item.Id, item => item.LastSeenAt);
        return new WhereIsResponse(
            matches.Select(match => match with { LastSeenAt = seen.GetValueOrDefault(match.Id) }).ToList(),
            found.Total ?? matches.Count);
    }

    public async Task<ItemResponse> ChangeItemAsync(Guid userId, Guid spaceId, Guid id, ItemChangesRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var type = await validation.ItemTypeAsync(spaceId, request.TypeId ?? item.TypeId, cancellationToken);
        var fields = new Dictionary<Guid, JsonElement>(item.Fields);
        foreach (var (key, text) in request.Fields ?? new Dictionary<string, string>())
        {
            var field = type?.Fields.FirstOrDefault(candidate => string.Equals(candidate.Key, key.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new RuleViolationException(type is null
                    ? $"{item.Name} has no type, so it has no fields. Give it a type first."
                    : $"{type.Name} has no field called {key}. Its fields are: {string.Join(", ", type.Fields.Select(candidate => candidate.Key))}.");
            if (string.IsNullOrWhiteSpace(text))
            {
                fields.Remove(field.Id);
            }
            else
            {
                fields[field.Id] = FieldValue(field, text)
                    ?? throw new RuleViolationException($"\"{text}\" isn't a value {field.Label} can hold ({field.Kind}{(field.Options.Count > 0 ? $": {string.Join(", ", field.Options)}" : string.Empty)}).");
            }
        }

        var update = new ItemRequest(
            request.Name ?? item.Name,
            request.Marking is null ? item.Marking : Blank(request.Marking),
            type?.Id,
            null,
            type?.Id == item.TypeId ? item.HoldsItems : null,
            item.Quantity,
            item.Unit,
            fields,
            request.Tags ?? item.Tags,
            request.Notes is null ? item.Notes : Blank(request.Notes),
            item.Version);
        return await UpdateItemAsync(userId, spaceId, id, update, cancellationToken);
    }

    /// <summary>Where a search result is, in words: its containers, the Inbox (no place and no type), or the top.</summary>
    private static string Where(SearchResult result) => result.Chain.Count > 0
        ? string.Join(" › ", result.Chain.Select(place => place.Name))
        : result.Item.TypeId is null && !result.Item.HoldsItems ? "the Inbox (no place yet)" : "the top of the space";

    /// <summary>A value given as text: an Item field takes the other item's id; every other kind is read as an AI answer is.</summary>
    private static JsonElement? FieldValue(TypeField field, string text) =>
        field.Kind == FieldKind.Item
            ? Guid.TryParse(text.Trim(), out var other) ? JsonSerializer.SerializeToElement(other.ToString(), AiAnswerJson.Default.String) : null
            : TransformationEngine.FieldValue(field, text);

    private static string? Blank(string text) => text.Trim().Length == 0 ? null : text;
}
