using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>An import of a CSV file into a space, and how far it has got.</summary>
internal sealed class Import
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SpaceId { get; set; }

    public ImportKind Kind { get; set; }

    public string FileName { get; set; } = string.Empty;

    /// <summary>Where the uploaded file is kept until the import finishes, or null once it is gone.</summary>
    public string? FilePath { get; set; }

    public List<ColumnMapping> Mapping { get; set; } = [];

    /// <summary>The container everything goes into, or null for the top.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>The type for rows that don't name one, or null.</summary>
    public Guid? TypeId { get; set; }

    public ImportStatus Status { get; set; }

    /// <summary>How many rows the file has, not counting the heading.</summary>
    public int Total { get; set; }

    /// <summary>How many rows have been read, so a run that stopped carries on after them.</summary>
    public int Done { get; set; }

    /// <summary>How many items were added, location containers included.</summary>
    public int Added { get; set; }

    public List<ImportProblem> Problems { get; set; } = [];

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Goes up with each change; the database refuses a save from an older version.</summary>
    public int Version { get; set; }
}
