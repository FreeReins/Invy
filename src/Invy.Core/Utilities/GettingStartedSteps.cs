namespace Invy.Core.Utilities;

/// <summary>
/// The steps a kind's getting-started card can list (its pack's <c>gettingStarted</c> keys). Whether each is done is
/// worked out from data the space already has, never stored.
/// </summary>
internal static class GettingStartedSteps
{
    public const string Places = "places";
    public const string Labels = "labels";
    public const string Pack = "pack";
    public const string Photo = "photo";
    public const string Invite = "invite";
    public const string Values = "values";
    public const string Audit = "audit";
    public const string Stock = "stock";
    public const string Lend = "lend";
    public const string Remind = "remind";
    public const string Destination = "destination";
    public const string Project = "project";

    public static readonly IReadOnlyList<Step> All =
    [
        new(Places, "Add your places", "Add the rooms, shelves or cupboards things live in.", Feature: null),
        new(Labels, "Print labels", "Print QR labels to stick on boxes, so a scan opens them.", Feature: null),
        new(Pack, "Put something away", "Add something inside a box or a place.", Feature: null),
        new(Photo, "Try a photo", "Add a photo to an item, or let AI list what's in a photo of a box.", Feature: null),
        new(Invite, "Invite someone", "Add someone who shares the space with you.", Feature: null),
        new(Values, "Give something a value", "Fill in a price or value, and see the totals add up.", Feature: "values"),
        new(Audit, "Check what's where", "Mark something as seen, or walk round with the audit.", Feature: "audits"),
        new(Stock, "Set a low-stock level", "Give something you keep counted a level, and get told when it runs low.", Feature: SpaceFeatures.Stock),
        new(Lend, "Lend something", "Mark something as lent, with who has it and when it's due back.", Feature: SpaceFeatures.Lending),
        new(Remind, "Add a warranty or upkeep date", "Fill in a date field that reminds you, such as Warranty until.", Feature: SpaceFeatures.Reminders),
        new(Destination, "Say where a box is going", "Give a packed box the room it goes to in the new place.", Feature: SpaceFeatures.Moving),
        new(Project, "Start a project", "Make a project and set aside what it needs.", Feature: SpaceFeatures.Projects),
    ];

    /// <summary>The step with this key, or null when there is none.</summary>
    public static Step? Find(string key) => All.FirstOrDefault(step => step.Key == key);

    /// <summary>One step.</summary>
    /// <param name="Key">What a pack lists, such as places.</param>
    /// <param name="Title">What the card shows.</param>
    /// <param name="Description">One plain line on how to do it.</param>
    /// <param name="Feature">The feature it belongs to, left off the card while that is off; null for none.</param>
    public sealed record Step(string Key, string Title, string Description, string? Feature);
}
