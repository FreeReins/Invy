using Invy.Core.Engines;
using Invy.Core.ResourceAccess;

namespace Invy.Core.Tests;

/// <summary>Values and engines shared by the tests.</summary>
internal static class TestData
{
    /// <summary>A Sunday in November.</summary>
    public static readonly DateOnly Sunday = new(2026, 11, 8);

    public static readonly NotificationEngine Notifications = new();

    /// <summary>The starter packs shipped in config/, copied next to the tests.</summary>
    public static readonly StarterPackAccess StarterPacks = new(Path.Combine(AppContext.BaseDirectory, "config"));

    /// <summary>The exception <paramref name="action"/> threw, or null when it didn't throw one of that type.</summary>
    public static async Task<TException?> Caught<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
            return null;
        }
        catch (TException exception)
        {
            return exception;
        }
    }
}
