using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Utilities;

/// <summary>What each <see cref="AiTask"/> is called on the page, and what its model needs to be able to do.</summary>
internal static class AiTasks
{
    public static string Label(AiTask task) => task switch
    {
        AiTask.ItemsFromPhoto => "Listing things in photos",
        AiTask.ItemDetailsFromPhoto => "Reading labels and nameplates",
        AiTask.SuggestTypeFields => "Suggesting fields for types",
        AiTask.SuggestType => "Suggesting types for the Inbox",
        _ => task.ToString(),
    };

    public static string Description(AiTask task) => task switch
    {
        AiTask.ItemsFromPhoto => "Lists what is in a photo of an open box, with types and field values, for you to tick. Needs a model that can see images.",
        AiTask.ItemDetailsFromPhoto => "Reads brand, model, serial and other fields from a photo of a label or nameplate. Needs a model that can see images.",
        AiTask.SuggestTypeFields => "Suggests the fields a new item type could have, such as set number and pieces for Lego sets.",
        AiTask.SuggestType => "Suggests a type for each item in the Inbox, in the background, for you to accept or ignore.",
        _ => string.Empty,
    };
}
