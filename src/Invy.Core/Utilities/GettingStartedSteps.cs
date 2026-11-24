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

    public static readonly IReadOnlyList<Step> All =
    [
        new(Places, "Add your places", "Add the rooms, shelves or cupboards things live in.", Feature: null),
        new(Labels, "Print labels", "Print QR labels to stick on boxes, so a scan opens them.", Feature: null),
        new(Pack, "Put something away", "Add something inside a box or a place.", Feature: null),
        new(Photo, "Try a photo", "Add a photo to an item, or let AI list what's in a photo of a box.", Feature: null),
        new(Invite, "Invite someone", "Add someone who shares the space with you.", Feature: null),
        new(Values, "Give something a value", "Fill in a price or value, and see the totals add up.", Feature: "values"),
        new(Audit, "Check what's where", "Mark something as seen, or walk round with the audit.", Feature: "audits"),
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
