namespace Invy.Core.Utilities;

/// <summary>
/// The features a space can switch on and off (docs/developers/design.md, Features). A space stores the keys it has
/// on. A feature that is off is hidden, not blocked. Never rename a key: stored spaces hold it.
/// </summary>
internal static class SpaceFeatures
{
    /// <summary>Every feature, in the order the switches are shown.</summary>
    public static readonly IReadOnlyList<Feature> All =
    [
        new("values", "Values", "Add up what things are worth, in each box and across the space.", Available: true),
        new("audits", "Audits", "Walk round with your phone and tick off what's where.", Available: true),
        new("barcodes", "Barcodes", "Fill in a product's details from its barcode when you add it.", Available: true),
        new("stock", "Stock", "Get told when something you keep counted is running low.", Available: false),
        new("lending", "Lending", "Lend things to people and get reminded when they're due back.", Available: false),
        new("reminders", "Reminders", "Get reminded of warranties, upkeep and expiry dates.", Available: false),
        new("moving", "Moving", "Pack boxes for a move, with where each one goes and what's in it.", Available: false),
        new("projects", "Projects", "Set things aside or use them up for a project.", Available: false),
    ];

    /// <summary>The feature with this key, or null when there is none.</summary>
    public static Feature? Find(string key) => All.FirstOrDefault(feature => feature.Key == key);

    /// <summary>One feature.</summary>
    /// <param name="Key">What spaces store, such as lending.</param>
    /// <param name="Name">What its switch is called.</param>
    /// <param name="Description">One plain line under the switch.</param>
    /// <param name="Available">True once the feature is built, so its switch is offered.</param>
    public sealed record Feature(string Key, string Name, string Description, bool Available);
}
