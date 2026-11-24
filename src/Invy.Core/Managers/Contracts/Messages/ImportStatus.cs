using System.Text.Json.Serialization;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Where an import is up to.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ImportStatus>))]
public enum ImportStatus
{
    /// <summary>Uploaded, waiting for its columns to be checked and the import started.</summary>
    Draft,

    /// <summary>Started, waiting its turn.</summary>
    Queued,

    /// <summary>Adding items now.</summary>
    Running,

    /// <summary>Finished. Rows that couldn't be read are listed as problems.</summary>
    Done,

    /// <summary>Stopped by a problem with the whole file.</summary>
    Failed,
}
