namespace Invy.Seed;

/// <summary>Words the seeded names, notes and tags are made of, so search has realistic text to match.</summary>
internal static class SeedWords
{
    public static readonly string[] Places =
    [
        "Garage", "Shed", "Attic", "Basement", "Kitchen", "Workshop", "Loft", "Office", "Pantry", "Storage unit",
        "Hall cupboard", "Spare room", "Studio", "Barn", "Warehouse", "Stockroom",
    ];

    public static readonly string[] Containers =
    [
        "Shelf", "Box", "Drawer", "Bin", "Crate", "Tote", "Cabinet", "Rack", "Case", "Tray", "Bag", "Chest",
    ];

    public static readonly string[] Adjectives =
    [
        "red", "blue", "green", "black", "yellow", "small", "large", "cordless", "vintage", "spare", "old", "new",
        "heavy", "folding", "electric", "manual", "steel", "wooden", "plastic", "brass", "rubber", "waterproof",
    ];

    public static readonly string[] Tools =
    [
        "drill", "hammer", "saw", "wrench", "screwdriver", "sander", "chisel", "clamp", "level", "tape measure",
        "pliers", "router", "jigsaw", "grinder", "heat gun", "soldering iron", "multimeter", "torch", "ladder",
        "socket set", "file", "plane", "mallet", "crowbar", "stapler", "glue gun", "vice", "spanner", "hacksaw",
    ];

    public static readonly string[] Parts =
    [
        "bolt", "nut", "washer", "screw", "nail", "hinge", "bracket", "spring", "rivet", "cable tie", "fuse",
        "resistor", "capacitor", "battery", "bulb", "o-ring", "zip", "button", "bead", "magnet", "anchor", "pin",
    ];

    public static readonly string[] BookWords =
    [
        "garden", "river", "winter", "kitchen", "history", "letters", "night", "journey", "island", "machine",
        "stars", "house", "ocean", "mountain", "city", "forest", "secret", "summer", "light", "road",
    ];

    public static readonly string[] Brands =
    [
        "DeWalt", "Makita", "Bosch", "Stanley", "Ryobi", "Milwaukee", "Festool", "Hitachi", "Black+Decker", "Draper",
        "Wera", "Bahco", "Irwin", "Knipex", "Fluke",
    ];

    public static readonly string[] Authors =
    [
        "Austen", "Tolkien", "Le Guin", "Pratchett", "Atwood", "Orwell", "Morrison", "Ishiguro", "Mantel", "Christie",
        "Dickens", "Woolf", "Carver", "Munro", "Gaiman",
    ];

    public static readonly string[] Tags =
    [
        "garden", "camping", "christmas", "car", "bike", "kids", "loan", "fragile", "insured", "spare", "repair",
        "keep", "sell", "gift", "seasonal",
    ];

    public static readonly string[] Notes =
    [
        "bought at the car boot sale", "needs a new battery", "lent to the neighbours once", "keep away from damp",
        "came with the house", "spare for the van", "check before winter", "warranty card in the folder",
        "missing one piece", "works but noisy", "from grandad's workshop", "the good one",
    ];

    public static string Pick(this Random random, string[] words) => words[random.Next(words.Length)];
}
