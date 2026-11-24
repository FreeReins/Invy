using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <content>Copies ProjectEntity to and from Project.</content>
internal static partial class EntityMapping
{
    public static Project ToContract(this ProjectEntity entity) => new()
    {
        Id = entity.Id,
        SpaceId = entity.SpaceId,
        Name = entity.Name,
        Notes = entity.Notes,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Version = entity.Version,
        Items = entity.Items.Select(item => new ProjectItem { ItemId = item.ItemId, Amount = item.Amount }).ToList(),
    };

    public static ProjectEntity ToEntity(this Project project) => new()
    {
        Id = project.Id,
        SpaceId = project.SpaceId,
        Name = project.Name,
        Notes = project.Notes,
        Status = project.Status,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt,
        Version = project.Version,
        Items = project.Items.Select(item => new ProjectItemEntity
        {
            ProjectId = project.Id,
            SpaceId = project.SpaceId,
            ItemId = item.ItemId,
            Amount = item.Amount,
        }).ToList(),
    };
}
