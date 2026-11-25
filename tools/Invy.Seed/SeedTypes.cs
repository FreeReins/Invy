using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Seed;

/// <summary>The seeded space's item types: places and boxes that hold things, and tools, parts and books with fields.</summary>
internal sealed class SeedTypes
{
    public SeedTypes(Guid spaceId)
    {
        Place = Type(spaceId, "Place", holdsItems: true);
        Box = Type(spaceId, "Box", holdsItems: true);
        Tool = Type(spaceId, "Tool", false, Field("brand", "Brand", FieldKind.Text), Field("price", "Price", FieldKind.Money, "USD"),
            Field("bought", "Bought", FieldKind.Date));
        Part = Type(spaceId, "Part", false, Field("size", "Size", FieldKind.Number, "mm"), Field("price", "Price", FieldKind.Money, "USD"));
        Book = Type(spaceId, "Book", false, Field("author", "Author", FieldKind.Text), Field("year", "Year", FieldKind.Number),
            Field("price", "Price", FieldKind.Money, "USD"));
    }

    public ItemType Place { get; }

    public ItemType Box { get; }

    public ItemType Tool { get; }

    public ItemType Part { get; }

    public ItemType Book { get; }

    public IReadOnlyList<ItemType> All => [Place, Box, Tool, Part, Book];

    public static Guid FieldId(ItemType type, string key) => type.Fields.Single(field => field.Key == key).Id;

    private static ItemType Type(Guid spaceId, string name, bool holdsItems, params TypeField[] fields) => new()
    {
        SpaceId = spaceId,
        Name = name,
        HoldsItems = holdsItems,
        Fields = [.. fields],
    };

    private static TypeField Field(string key, string label, FieldKind kind, string? unit = null) => new()
    {
        Id = Guid.CreateVersion7(),
        Key = key,
        Label = label,
        Kind = kind,
        Unit = unit,
        Sortable = true,
    };
}
