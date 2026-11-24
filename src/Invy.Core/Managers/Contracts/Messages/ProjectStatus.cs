using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Whether a project is still going. A done project holds nothing back.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectStatus>))]
public enum ProjectStatus
{
    Open,
    Done,
}
