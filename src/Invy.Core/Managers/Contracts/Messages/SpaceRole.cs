using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What a member can do in a space. Each role can do everything the ones before it can.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SpaceRole>))]
public enum SpaceRole
{
    /// <summary>Can search and look.</summary>
    Viewer,

    /// <summary>Can add, change, move and remove items, types and views.</summary>
    Editor,

    /// <summary>Can do everything, including managing members and deleting the space.</summary>
    Owner,
}
