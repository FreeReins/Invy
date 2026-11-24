using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What kind of file an import reads.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ImportKind>))]
public enum ImportKind
{
    /// <summary>Any CSV file, with its columns mapped by hand.</summary>
    Csv,

    /// <summary>Homebox's CSV export (columns starting HB.), mapped for you.</summary>
    Homebox,
}
