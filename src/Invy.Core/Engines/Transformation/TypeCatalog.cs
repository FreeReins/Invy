using System.Text.Json.Nodes;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines.Transformation;

/// <summary>A space's item types and fields as a model reads them, and the ids its answer may use.</summary>
internal static class TypeCatalog
{
    /// <summary>The most types described in one question. A space with more sends the first ones by name.</summary>
    public const int MaxTypes = 150;

    /// <summary>The types to describe: by name, at most <see cref="MaxTypes"/>.</summary>
    public static List<ItemType> Pick(IEnumerable<ItemType> types) =>
        types.OrderBy(type => type.Name, StringComparer.CurrentCultureIgnoreCase).Take(MaxTypes).ToList();

    /// <summary>The types as JSON: id, name, whether it holds items, and its fields (Item fields left out: a model can't fill them).</summary>
    public static string Describe(IEnumerable<ItemType> types)
    {
        var list = new JsonArray();
        foreach (var type in types)
        {
            var fields = new JsonArray();
            foreach (var field in Fillable(type))
            {
                var described = new JsonObject { ["id"] = field.Id.ToString(), ["label"] = field.Label, ["kind"] = field.Kind.ToString() };
                if (field.Unit is { } unit)
                {
                    described["unit"] = unit;
                }

                if (field.Options.Count > 0)
                {
                    described["options"] = new JsonArray([.. field.Options.Select(option => (JsonNode)option)]);
                }

                fields.Add(described);
            }

            list.Add(new JsonObject
            {
                ["id"] = type.Id.ToString(),
                ["name"] = type.Name,
                ["holdsItems"] = type.HoldsItems,
                ["fields"] = fields,
            });
        }

        return list.ToJsonString();
    }

    /// <summary>The fields a model can give a value for.</summary>
    public static IEnumerable<TypeField> Fillable(ItemType type) => type.Fields.Where(field => field.Kind != FieldKind.Item);

    /// <summary>A JSON schema enum of these ids, plus "" for none.</summary>
    public static string IdEnum(IEnumerable<Guid> ids) =>
        "[" + string.Join(", ", ids.Select(id => $"\"{id}\"").Append("\"\"")) + "]";
}
