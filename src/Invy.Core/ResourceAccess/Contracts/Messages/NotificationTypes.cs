namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>The kinds of notification Invy sends. The value is the <c>type</c> field of the webhook payload.</summary>
internal static class NotificationTypes
{
    /// <summary>Sent from the Settings tab to check the webhook works.</summary>
    public const string Test = "test";
}
