using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Item types kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed class FakeTypes : ITypeAccess
{
    public List<ItemType> Rows { get; } = [];

    public Task<List<ItemType>> ListAsync(Guid spaceId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(type => type.SpaceId == spaceId)
            .OrderBy(type => type.Name, StringComparer.CurrentCultureIgnoreCase).Select(Copy).ToList());

    public Task<ItemType?> FindAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(type => type.SpaceId == spaceId && type.Id == id) is { } type ? Copy(type) : null);

    public Task SaveAsync(ItemType type, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == type.Id);
        Rows.Add(Copy(type));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.SpaceId == spaceId && row.Id == id);
        return Task.CompletedTask;
    }

    private static ItemType Copy(ItemType type) => new()
    {
        Id = type.Id,
        SpaceId = type.SpaceId,
        Name = type.Name,
        Icon = type.Icon,
        Colour = type.Colour,
        HoldsItems = type.HoldsItems,
        Version = type.Version,
        Fields = type.Fields.Select(field => new TypeField
        {
            Id = field.Id,
            Key = field.Key,
            Label = field.Label,
            Kind = field.Kind,
            Unit = field.Unit,
            Options = [.. field.Options],
            Required = field.Required,
            Sortable = field.Sortable,
        }).ToList(),
    };
}
