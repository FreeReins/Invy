namespace Invy.Core.Managers.Contracts;

/// <summary>
/// The durable local queues the message handlers run on, set on each handler with
/// <c>[StickyHandler(MessageQueues.…)]</c> and configured in <see cref="CoreRegistration"/>. A queue groups the
/// handlers that must not run at the same time as each other, or that call the same outside service.
/// </summary>
public static class MessageQueues
{
    /// <summary>Works on photos, a couple at a time.</summary>
    public const string Images = "images";

    /// <summary>Delivers notifications to the webhook, a couple at a time.</summary>
    public const string Notifications = "notifications";

    /// <summary>
    /// Adds background news to people's inboxes: one message at a time, so
    /// the same news is never added twice.
    /// </summary>
    public const string Inbox = "inbox";

    /// <summary>The scheduled jobs, backups and settings that apply to the running server: one at a time.</summary>
    public const string Maintenance = "maintenance";

    /// <summary>
    /// Questions for AI models, such as reading a photo: one at a time, to stay within the provider's
    /// rate limits and keep a home server's model from being swamped.
    /// </summary>
    public const string Ai = "ai";

    /// <summary>
    /// Recounts containers' contents and value totals after items change: one at a time, so two recounts of the same
    /// container never race.
    /// </summary>
    public const string Rollups = "rollups";

    /// <summary>Runs imports, one at a time, so two never add the same locations at once.</summary>
    public const string Imports = "imports";

    /// <summary>
    /// Tells open pages what changed. Kept in memory only: a page that misses a nudge loads everything again when it
    /// reconnects, so these messages are not worth storing.
    /// </summary>
    public const string Live = "live";
}
