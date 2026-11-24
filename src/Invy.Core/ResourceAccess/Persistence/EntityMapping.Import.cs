using System.Text.Json;
using System.Text.Json.Serialization;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies ImportEntity to and from Import.</content>
internal static partial class EntityMapping
{
    public static Import ToContract(this ImportEntity entity) => new()
    {
        Id = entity.Id,
        SpaceId = entity.SpaceId,
        Kind = entity.Kind,
        FileName = entity.FileName,
        FilePath = entity.FilePath,
        Mapping = JsonSerializer.Deserialize(entity.Mapping, ImportJson.Default.ListColumnMapping) ?? [],
        ParentId = entity.ParentId,
        TypeId = entity.TypeId,
        Status = entity.Status,
        Total = entity.Total,
        Done = entity.Done,
        Added = entity.Added,
        Problems = JsonSerializer.Deserialize(entity.Problems, ImportJson.Default.ListImportProblem) ?? [],
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt,
        FinishedAt = entity.FinishedAt,
        Version = entity.Version,
    };

    public static ImportEntity ToEntity(this Import import) => new()
    {
        Id = import.Id,
        SpaceId = import.SpaceId,
        Kind = import.Kind,
        FileName = import.FileName,
        FilePath = import.FilePath,
        Mapping = JsonSerializer.Serialize(import.Mapping, ImportJson.Default.ListColumnMapping),
        ParentId = import.ParentId,
        TypeId = import.TypeId,
        Status = import.Status,
        Total = import.Total,
        Done = import.Done,
        Added = import.Added,
        Problems = JsonSerializer.Serialize(import.Problems, ImportJson.Default.ListImportProblem),
        CreatedBy = import.CreatedBy,
        CreatedAt = import.CreatedAt,
        FinishedAt = import.FinishedAt,
        Version = import.Version,
    };

    /// <summary>The JSON stored in imports' jsonb columns, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<ColumnMapping>))]
    [JsonSerializable(typeof(List<ImportProblem>))]
    private sealed partial class ImportJson : JsonSerializerContext;
}
