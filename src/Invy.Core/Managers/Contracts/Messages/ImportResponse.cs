namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An import: its file's columns, how they are mapped, and how far it has got.</summary>
/// <param name="Id">The import's id.</param>
/// <param name="Kind">Any CSV, or Homebox's export.</param>
/// <param name="FileName">The uploaded file's name.</param>
/// <param name="Status">Where it is up to.</param>
/// <param name="Headers">The file's heading row. Empty once the import has finished and its file is gone.</param>
/// <param name="Sample">Up to 5 rows from the start of the file, to check the mapping against.</param>
/// <param name="Mapping">Where each column goes: suggested after uploading, as started afterwards.</param>
/// <param name="ParentId">The container everything goes into, or null for the top.</param>
/// <param name="TypeId">The type for rows that don't name one, or null.</param>
/// <param name="Total">How many rows the file has, not counting the heading.</param>
/// <param name="Done">How many rows have been read so far.</param>
/// <param name="Added">How many items were added (location containers included).</param>
/// <param name="Problems">Rows skipped and why, the first 100.</param>
/// <param name="CreatedAt">When it was uploaded.</param>
/// <param name="FinishedAt">When it finished, or null.</param>
public sealed record ImportResponse(
    Guid Id,
    ImportKind Kind,
    string FileName,
    ImportStatus Status,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Sample,
    IReadOnlyList<ColumnMapping> Mapping,
    Guid? ParentId,
    Guid? TypeId,
    int Total,
    int Done,
    int Added,
    IReadOnlyList<ImportProblem> Problems,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinishedAt);
