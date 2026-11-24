using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Spaces kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed class FakeSpaces : ISpaceAccess
{
    public List<Space> Rows { get; } = [];

    public Task<Space?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(space => space.Id == id) is { } space ? Copy(space) : null);

    public Task<List<Space>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(space => space.Members.Any(member => member.UserId == userId))
            .OrderBy(space => space.CreatedAt).ThenBy(space => space.Id).Select(Copy).ToList());

    public Task<List<Space>> ListWithFeatureAsync(string feature, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(space => space.Features.Contains(feature)).OrderBy(space => space.Name).ThenBy(space => space.Id).Select(Copy).ToList());

    public Task SaveAsync(Space space, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == space.Id);
        Rows.Add(Copy(space));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == id);
        return Task.CompletedTask;
    }

    private static Space Copy(Space space) => new()
    {
        Id = space.Id,
        Name = space.Name,
        Preset = space.Preset,
        CreatedAt = space.CreatedAt,
        Features = [.. space.Features],
        CalendarToken = space.CalendarToken,
        Members = space.Members
            .Select(member => new SpaceMember { UserId = member.UserId, Role = member.Role, GettingStartedDismissed = member.GettingStartedDismissed })
            .ToList(),
    };
}
