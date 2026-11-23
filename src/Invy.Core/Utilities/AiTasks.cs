using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Utilities;

/// <summary>What each <see cref="AiTask"/> is called on the page, and what its model needs to be able to do.</summary>
internal static class AiTasks
{
    public static string Label(AiTask task) => task switch
    {
        _ => task.ToString(),
    };

    public static string Description(AiTask task) => task switch
    {
        _ => string.Empty,
    };
}
