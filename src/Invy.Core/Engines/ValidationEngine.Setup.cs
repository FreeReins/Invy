using System.Text.Json;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Engines;

/// <content>Checks on what a new space starts with: its features, places and examples.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>The most places setup makes at once.</summary>
    public const int MaxPlaces = 30;

    /// <summary>Feature keys, trimmed, in lower case, once each, in the catalogue's order.</summary>
    /// <exception cref="RuleViolationException">A key isn't one of <see cref="SpaceFeatures.All"/>.</exception>
    public static List<string> Features(IEnumerable<string?>? keys)
    {
        var wanted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys ?? [])
        {
            var trimmed = (key ?? string.Empty).Trim().ToLowerInvariant();
            if (SpaceFeatures.Find(trimmed) is null)
            {
                throw new RuleViolationException(
                    $"There is no feature called {trimmed}. Pick from {string.Join(", ", SpaceFeatures.All.Select(feature => feature.Key))}.");
            }

            wanted.Add(trimmed);
        }

        return SpaceFeatures.All.Select(feature => feature.Key).Where(wanted.Contains).ToList();
    }

    /// <summary>The names of the places to make, trimmed, each once whatever its case, in the order given.</summary>
    public static List<string> PlaceNames(IEnumerable<string?>? names)
    {
        var places = (names ?? [])
            .Select(name => Trimmed(name ?? string.Empty))
            .Where(name => name.Length > 0)
            .DistinctBy(name => name.ToLowerInvariant())
            .ToList();
        if (places.Any(name => name.Length > ItemNameLength))
        {
            throw new RuleViolationException($"A place needs a name of 1 to {ItemNameLength} characters.");
        }

        return places.Count <= MaxPlaces
            ? places
            : throw new RuleViolationException($"Setup can make up to {MaxPlaces} places at once. Add the rest in Browse afterwards.");
    }

    /// <summary>The pack's type called <paramref name="name"/>, or null for none.</summary>
    /// <exception cref="RuleViolationException">The pack names a type it doesn't have.</exception>
    public static ItemType? PackType(StarterPack pack, IReadOnlyDictionary<string, ItemType> types, string? name) =>
        name is null ? null
        : types.TryGetValue(name, out var type) ? type
        : throw new RuleViolationException($"The {pack.Name} starter pack uses a type called {name} that it doesn't have. Pick another pack, or none.");

    /// <summary>An example as an item request, so it goes through the same checks as an item added by hand.</summary>
    /// <exception cref="RuleViolationException">The example has a field its type doesn't have.</exception>
    public static ItemRequest ExampleRequest(StarterPack pack, StarterExample example, ItemType? type, Guid? parentId)
    {
        var fields = new Dictionary<Guid, JsonElement>();
        foreach (var (key, value) in example.Fields)
        {
            var field = type?.Fields.FirstOrDefault(field => field.Key == key)
                ?? throw new RuleViolationException(
                    $"The {pack.Name} starter pack's example {example.Name} has a field {key} that its type doesn't have. Make the space without examples.");
            fields[field.Id] = value;
        }

        return new ItemRequest(example.Name, null, type?.Id, parentId, null, example.Quantity, example.Unit, fields, null, null, 0, example.LowStockAt);
    }
}
