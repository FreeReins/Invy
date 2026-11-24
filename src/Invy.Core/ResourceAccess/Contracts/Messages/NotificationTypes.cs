namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>The kinds of notification Invy sends. The value is the <c>type</c> field of the webhook payload.</summary>
internal static class NotificationTypes
{
    /// <summary>Sent from the Settings tab to check the webhook works.</summary>
    public const string Test = "test";

    /// <summary>Sent right away when a counted item reaches its low-stock level.</summary>
    public const string LowStock = "low-stock";

    /// <summary>The daily list of everything still low.</summary>
    public const string LowStockDaily = "low-stock-daily";

    /// <summary>The daily list of loans due back today or overdue.</summary>
    public const string Overdue = "overdue";

    /// <summary>The daily list of warranty, upkeep and other reminder dates coming up or due.</summary>
    public const string Reminders = "reminders";
}
