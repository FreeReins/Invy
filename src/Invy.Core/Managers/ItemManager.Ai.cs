using Invy.Core.Engines;
using Invy.Core.Engines.Transformation;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Suggestions from AI models: things in a photo, fields from a nameplate, types for the Inbox. None is saved as an item change on its own.</content>
internal sealed partial class ItemManager
{
    /// <summary>How many Inbox items one question asks about. A bigger Inbox is asked about a batch at a time.</summary>
    public const int SuggestTypeBatch = 40;

    /// <summary>How long after things land in the Inbox their types are asked for, so a run of quick adds is asked about once.</summary>
    public static readonly TimeSpan SuggestTypesAfter = TimeSpan.FromMinutes(2);

    public async Task<ItemSuggestionsResponse> SuggestFromPhotoAsync(Guid userId, Guid spaceId, PhotoRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var photos = ValidationEngine.AiPhotos(request);
        var model = ValidationEngine.AiModelFor(AiTask.ItemsFromPhoto, await settings.GetAiAsync(cancellationToken));
        var spaceTypes = TypeCatalog.Pick(await types.ListAsync(spaceId, cancellationToken));

        var answer = await ai.AskAsync(model, TransformationEngine.ItemsFromPhotoQuestion(photos, spaceTypes), cancellationToken);
        return new ItemSuggestionsResponse(TransformationEngine.ReadItemSuggestions(answer, spaceTypes), model.Name);
    }

    public async Task<DetailSuggestionsResponse> SuggestDetailsFromPhotoAsync(
        Guid userId, Guid spaceId, Guid id, PhotoRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        var type = await validation.ItemTypeAsync(spaceId, item.TypeId, cancellationToken);
        if (type is null || !TypeCatalog.Fillable(type).Any())
        {
            throw new RuleViolationException($"{item.Name} has no fields to fill in. Give it a type with fields such as Brand or Serial first.");
        }

        var photos = ValidationEngine.AiPhotos(request);
        var model = ValidationEngine.AiModelFor(AiTask.ItemDetailsFromPhoto, await settings.GetAiAsync(cancellationToken));
        var answer = await ai.AskAsync(model, TransformationEngine.ItemDetailsQuestion(photos, item.Name, type), cancellationToken);
        return new DetailSuggestionsResponse(TransformationEngine.ReadItemDetails(answer, type), model.Name);
    }

    public async Task SuggestInboxTypesAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        ValidationEngine.AiModelFor(AiTask.SuggestType, await settings.GetAiAsync(cancellationToken));
        if ((await types.ListAsync(spaceId, cancellationToken)).Count == 0)
        {
            throw new RuleViolationException("This space has no types to suggest yet. Add some on the Types tab first.");
        }

        await messages.PublishAsync(new SuggestInboxTypes(spaceId, clock.GetUtcNow()));
        await messages.CommitAsync(cancellationToken);
    }

    public async Task SweepInboxTypesAsync(SuggestInboxTypes message, CancellationToken cancellationToken)
    {
        if (ValidationEngine.AiModelOrNull(AiTask.SuggestType, await settings.GetAiAsync(cancellationToken)) is not { } model)
        {
            return;
        }

        var spaceTypes = TypeCatalog.Pick(await types.ListAsync(message.SpaceId, cancellationToken));
        var batch = spaceTypes.Count == 0
            ? []
            : await items.ListInboxToSuggestAsync(message.SpaceId, message.AskedAgainAt, SuggestTypeBatch, cancellationToken);
        if (batch.Count == 0)
        {
            return;
        }

        var answer = await ai.AskAsync(model, TransformationEngine.InboxTypesQuestion(batch, spaceTypes), cancellationToken);
        var suggested = TransformationEngine.ReadInboxTypes(answer, batch, spaceTypes);
        await items.SuggestTypesAsync(message.SpaceId, suggested, clock.GetUtcNow(), cancellationToken);
        if (suggested.Values.Any(typeId => typeId is not null))
        {
            await messages.PublishAsync(new InboxTypesSuggested(message.SpaceId));
        }

        if (batch.Count == SuggestTypeBatch)
        {
            // Each item asked about is now marked asked, so the next batch is new ones and the sweep ends.
            await messages.PublishAsync(message);
        }

        await messages.CommitAsync(cancellationToken);
    }

    public async Task<ItemResponse> AcceptSuggestedTypeAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        if (item.SuggestedTypeId is not Guid typeId)
        {
            throw new RuleViolationException($"{item.Name} has no suggested type any more. Choose a type on its page instead.");
        }

        if (await types.FindAsync(spaceId, typeId, cancellationToken) is null)
        {
            await items.SuggestTypesAsync(spaceId, new Dictionary<Guid, Guid?> { [id] = null }, clock.GetUtcNow(), cancellationToken);
            await messages.CommitAsync(cancellationToken);
            throw new RuleViolationException($"The type suggested for {item.Name} has been removed. Choose a type on its page instead.");
        }

        var request = new ItemRequest(
            item.Name, item.Marking, typeId, null, null, item.Quantity, item.Unit, item.Fields, item.Tags, item.Notes, item.Version, item.LowStockAt);
        return await UpdateItemAsync(userId, spaceId, id, request, cancellationToken);
    }

    public async Task<ItemResponse> IgnoreSuggestedTypeAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var item = await validation.ItemAsync(spaceId, id, cancellationToken);
        if (item.SuggestedTypeId is not null)
        {
            var now = clock.GetUtcNow();
            await items.SuggestTypesAsync(spaceId, new Dictionary<Guid, Guid?> { [id] = null }, now, cancellationToken);
            await messages.CommitAsync(cancellationToken);
            item.SuggestedTypeId = null;
            item.TypeSuggestedAt = now;
        }

        return item.ToResponse();
    }

    /// <summary>
    /// Asks for types shortly after, when any of <paramref name="added"/> landed in the Inbox and a model is set up.
    /// Asking twice is harmless: the sweep skips items already asked about.
    /// </summary>
    private async Task SuggestTypesLaterAsync(Guid spaceId, IReadOnlyList<Item> added, CancellationToken cancellationToken)
    {
        if (added.Any(item => item.ParentId is null && item.TypeId is null && !item.HoldsItems)
            && ValidationEngine.AiModelOrNull(AiTask.SuggestType, await settings.GetAiAsync(cancellationToken)) is not null)
        {
            await messages.ScheduleAsync(new SuggestInboxTypes(spaceId), clock.GetUtcNow() + SuggestTypesAfter);
        }
    }
}
