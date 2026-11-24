using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// A job Invy gives an AI model. Each one uses the default model unless another is chosen for it. The tasks come
/// with the features that use them (see the <c>add-ai-task</c> skill).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AiTask>))]
public enum AiTask
{
    /// <summary>Listing what is in a photo of an open box, matched to the space's types and fields. Needs a model that can see images.</summary>
    ItemsFromPhoto,

    /// <summary>Reading field values such as brand, model and serial from a photo of a label or nameplate. Needs a model that can see images.</summary>
    ItemDetailsFromPhoto,

    /// <summary>Suggesting the fields a new item type could have, from its name.</summary>
    SuggestTypeFields,

    /// <summary>Suggesting a type for each item in the Inbox, in the background.</summary>
    SuggestType,
}
