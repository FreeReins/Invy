namespace Invy.Api.Live;

/// <summary>The topics <see cref="LiveUpdates"/> sends, each matching what one part of the page loads.</summary>
public static class LiveTopics
{
    /// <summary>The backups list, after a backup finished.</summary>
    public const string Backups = "backups";

    /// <summary>
    /// Someone's inbox, the messages behind the bell. Sent to every page; each page loads only its own person's
    /// inbox, so a page whose inbox didn't change loads the same list again.
    /// </summary>
    public const string Inbox = "inbox";

    /// <summary>
    /// A space's Inbox items, after an AI model suggested types for them. Sent to every page, like <see cref="Inbox"/>;
    /// a page showing another space's Inbox loads the same list again.
    /// </summary>
    public const string InboxItems = "inbox-items";

    /// <summary>
    /// A space's items, after things were added, moved, seen or removed. Sent to every page; a page showing another
    /// space loads the same thing again. The getting-started card on Find and open views listen for it.
    /// </summary>
    public const string Items = "items";
}
