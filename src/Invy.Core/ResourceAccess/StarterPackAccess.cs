using System.Text.Json;
using System.Text.Json.Serialization;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.ResourceAccess;

/// <summary>The starter packs, read once from the <c>types-*.json</c> files in a folder (config/).</summary>
internal sealed partial class StarterPackAccess(string folder) : IStarterPackAccess
{
    private const string Prefix = "types-";

    private readonly Lazy<IReadOnlyList<StarterPack>> all = new(() => Load(folder));

    public IReadOnlyList<StarterPack> All() => all.Value;

    /// <summary>
    /// Reads one file shaped like
    /// <c>{ "name": "Home", "description": "...", "position": 1, "types": [ { "name": "Box", "holdsItems": true, "fields": [ { "key": "size", "label": "Size", "kind": "Select", "options": [...] } ] } ] }</c>.
    /// </summary>
    public static (StarterPack Pack, int Position) FromJson(string key, string json)
    {
        var file = JsonSerializer.Deserialize(json, PackJson.Default.PackFile)
            ?? throw new InvalidOperationException($"The starter pack {key} is empty.");
        var pack = new StarterPack
        {
            Key = key,
            Name = file.Name,
            Description = file.Description,
            Types = file.Types.Select(type => new ItemType
            {
                Name = type.Name,
                Icon = type.Icon,
                Colour = type.Colour,
                HoldsItems = type.HoldsItems,
                Fields = type.Fields.Select(field => new TypeField
                {
                    Key = field.Key,
                    Label = field.Label,
                    Kind = field.Kind,
                    Unit = field.Unit,
                    Options = [.. field.Options],
                    Required = field.Required,
                    Sortable = field.Sortable,
                }).ToList(),
            }).ToList(),
        };
        return (pack, file.Position);
    }

    private static IReadOnlyList<StarterPack> Load(string folder) =>
        Directory.Exists(folder)
            ? Directory.GetFiles(folder, $"{Prefix}*.json")
                .Select(path => FromJson(Path.GetFileNameWithoutExtension(path)[Prefix.Length..], File.ReadAllText(path)))
                .OrderBy(entry => entry.Position).ThenBy(entry => entry.Pack.Key, StringComparer.Ordinal)
                .Select(entry => entry.Pack)
                .ToList()
            : [];

    /// <summary>A whole pack file. Lists turn a missing or null value into an empty one.</summary>
    internal sealed record PackFile
    {
        public required string Name { get; init; }

        public string Description { get; init => field = value ?? string.Empty; } = string.Empty;

        public int Position { get; init; }

        public IReadOnlyList<TypeEntry> Types { get; init => field = value ?? []; } = [];
    }

    /// <summary>One type as the file writes it.</summary>
    internal sealed record TypeEntry
    {
        public required string Name { get; init; }

        public string? Icon { get; init; }

        public string? Colour { get; init; }

        public bool HoldsItems { get; init; }

        public IReadOnlyList<FieldEntry> Fields { get; init => field = value ?? []; } = [];
    }

    /// <summary>One field as the file writes it. See <see cref="FieldDefinition"/>.</summary>
    internal sealed record FieldEntry
    {
        public required string Key { get; init; }

        public required string Label { get; init; }

        public FieldKind Kind { get; init; }

        public string? Unit { get; init; }

        public IReadOnlyList<string> Options { get; init => field = value ?? []; } = [];

        public bool Required { get; init; }

        public bool Sortable { get; init; }
    }

    /// <summary>The pack file's JSON shape, made at build time. Kinds are written by name.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(PackFile))]
    private sealed partial class PackJson : JsonSerializerContext;
}
