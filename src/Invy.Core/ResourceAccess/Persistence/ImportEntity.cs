using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ImportEntity
{
    public Guid Id { get; set; }

    public Guid SpaceId { get; set; }

    public ImportKind Kind { get; set; }

    public string FileName { get; set; } = string.Empty;

    /// <summary>Where the uploaded file is kept until the import finishes, or null once it is gone.</summary>
    public string? FilePath { get; set; }

    /// <summary>The column mapping, as JSON in a jsonb column.</summary>
    public string Mapping { get; set; } = "[]";

    public Guid? ParentId { get; set; }

    public Guid? TypeId { get; set; }

    public ImportStatus Status { get; set; }

    public int Total { get; set; }

    public int Done { get; set; }

    public int Added { get; set; }

    /// <summary>The rows skipped, as JSON in a jsonb column.</summary>
    public string Problems { get; set; } = "[]";

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public int Version { get; set; }
}
