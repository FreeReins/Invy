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
}
