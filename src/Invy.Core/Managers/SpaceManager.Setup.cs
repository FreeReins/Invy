using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Making a space from its kind, and changing its kind and features afterwards.</content>
internal sealed partial class SpaceManager
{
    public async Task<SpaceResponse> CreateSpaceAsync(Guid userId, CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        await validation.UserAsync(userId, cancellationToken);
        var pack = validation.StarterPack(request.Preset);
        var now = clock.GetUtcNow();
        var space = new Space
        {
            Name = ValidationEngine.SpaceName(request.Name),
            Preset = pack?.Key,
            CreatedAt = now,
            Features = ValidationEngine.Features(request.Features ?? pack?.Features),
            Members = [new SpaceMember { UserId = userId, Role = SpaceRole.Owner }],
        };
        var placeNames = ValidationEngine.PlaceNames(request.Places);
        await spaces.SaveAsync(space, cancellationToken);

        // The pack's types go through the same checks as types made by hand, and get their own ids here.
        var made = new Dictionary<string, ItemType>(StringComparer.OrdinalIgnoreCase);
        foreach (var template in pack?.Types ?? [])
        {
            var type = ValidationEngine.TypeDraft(space.Id, Guid.CreateVersion7(), template.ToRequest());
            type.Version = 1;
            await types.SaveAsync(type, cancellationToken);
            made[type.Name] = type;
        }

        var setup = new Setup(space.Id, userId, now, pack is null ? null : ValidationEngine.PackType(pack, made, pack.Places?.Type));
        foreach (var name in placeNames)
        {
            await PlaceAsync(setup, name, isExample: false, cancellationToken);
        }

        if (request.Examples && pack is not null)
        {
            foreach (var example in pack.Examples)
            {
                var place = example.Place is null ? null
                    : setup.Places.GetValueOrDefault(example.Place) ?? await PlaceAsync(setup, example.Place, isExample: true, cancellationToken);
                await ExampleAsync(setup, pack, made, example, place, cancellationToken);
            }
        }

        var containers = setup.Added.Where(item => item.HoldsItems).Select(item => item.Id).ToList();
        if (containers.Count > 0)
        {
            await messages.PublishAsync(new ItemsChanged(space.Id, containers));
        }

        await messages.CommitAsync(cancellationToken);
        return space.ToResponse(userId);
    }

    public async Task<SpaceResponse> SetFeaturesAsync(Guid userId, Guid spaceId, SpaceFeaturesRequest request, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        space.Features = ValidationEngine.Features(request.Features);
        await spaces.SaveAsync(space, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return space.ToResponse(userId);
    }

    public async Task<SpaceResponse> SetKindAsync(Guid userId, Guid spaceId, SpaceKindRequest request, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        space.Preset = validation.StarterPack(request.Kind)?.Key;
        await spaces.SaveAsync(space, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return space.ToResponse(userId);
    }

    public async Task<RemoveExamplesResponse> RemoveExamplesAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var removed = await items.RemoveExamplesAsync(spaceId, cancellationToken);
        // The containers above what went, that are left, need recounting. An item's own id is the last in its path.
        var gone = removed.Select(path => ItemPaths.Ids(path)[^1]).ToHashSet();
        var containers = removed.SelectMany(ItemPaths.AncestorIds).Where(id => !gone.Contains(id)).Distinct().ToList();
        if (containers.Count > 0)
        {
            await messages.PublishAsync(new ItemsChanged(spaceId, containers));
        }

        await messages.CommitAsync(cancellationToken);
        return new RemoveExamplesResponse(removed.Count);
    }

    /// <summary>A place at the top, of the kind's place type when it has one.</summary>
    private async Task<Item> PlaceAsync(Setup setup, string name, bool isExample, CancellationToken cancellationToken)
    {
        var request = new ItemRequest(name, null, setup.PlaceType?.Id, null, true, null, null, null, null, null, 0);
        var place = await AddAsync(setup, request, setup.PlaceType, null, isExample, cancellationToken);
        setup.Places[name] = place;
        return place;
    }

    /// <summary>An example and what is inside it, at any depth.</summary>
    private async Task ExampleAsync(
        Setup setup, StarterPack pack, IReadOnlyDictionary<string, ItemType> made, StarterExample example, Item? parent,
        CancellationToken cancellationToken)
    {
        var type = ValidationEngine.PackType(pack, made, example.Type);
        var item = await AddAsync(setup, ValidationEngine.ExampleRequest(pack, example, type, parent?.Id), type, parent, isExample: true, cancellationToken);
        if (example.Contents.Count > 0 && !item.HoldsItems)
        {
            throw new RuleViolationException(
                $"The {pack.Name} starter pack's example {example.Name} has things inside it but can't hold items. Make the space without examples.");
        }

        foreach (var content in example.Contents)
        {
            await ExampleAsync(setup, pack, made, content, item, cancellationToken);
        }
    }

    /// <summary>Adds one item as <c>ItemManager</c> would: the same checks, its value and sortable values, and a Created event.</summary>
    private async Task<Item> AddAsync(
        Setup setup, ItemRequest request, ItemType? type, Item? parent, bool isExample, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            SpaceId = setup.SpaceId,
            Status = ItemStatus.Active,
            CreatedAt = setup.At,
            UpdatedAt = setup.At,
            Version = 1,
            IsExample = isExample,
        };
        ValidationEngine.ItemDetails(item, request, type);
        item.Fields = await validation.FieldValuesAsync(setup.SpaceId, type, request.Fields, item.Fields, cancellationToken);
        ItemEngine.Place(item, parent);
        item.Value = itemEngine.Value(item, type);
        await items.SaveAsync(item, itemEngine.SortableValues(item, type), cancellationToken);
        await items.AddEventAsync(ItemEngine.Event(item, setup.UserId, ItemEventKind.Created, setup.At, to: parent?.Id), cancellationToken);
        setup.Added.Add(item);
        return item;
    }

    /// <summary>What a new space's places and examples are being made with.</summary>
    private sealed record Setup(Guid SpaceId, Guid UserId, DateTimeOffset At, ItemType? PlaceType)
    {
        /// <summary>The places made so far, by name whatever its case.</summary>
        public Dictionary<string, Item> Places { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<Item> Added { get; } = [];
    }
}
