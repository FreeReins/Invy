using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Labels kept in memory. Each call hands out copies, as the database would.</summary>
internal sealed class FakeLabels : ILabelAccess
{
    public List<Label> Rows { get; } = [];

    public Label Code(string code) => Rows.Single(label => label.Code == code);

    public Task<Label?> FindAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(label => label.Code == code) is { } label ? Copy(label) : null);

    public Task<HashSet<string>> ExistingAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Select(label => label.Code).Where(codes.Contains).ToHashSet());

    public Task<int> CountAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Count);

    public Task<Label?> FindForItemAsync(Guid spaceId, Guid itemId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(label => label.SpaceId == spaceId && label.ItemId == itemId) is { } label ? Copy(label) : null);

    public Task<List<Label>> ListAsync(Guid spaceId, bool unclaimed, string? after, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(label => label.SpaceId == spaceId && (!unclaimed || label.ItemId is null))
            .Where(label => after is null || string.CompareOrdinal(label.Code, after) > 0)
            .OrderBy(label => label.Code, StringComparer.Ordinal).Take(limit).Select(Copy).ToList());

    public Task SaveAsync(Label label, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Code == label.Code);
        Rows.Add(Copy(label));
        return Task.CompletedTask;
    }

    private static Label Copy(Label label) => new()
    {
        Code = label.Code,
        SpaceId = label.SpaceId,
        ItemId = label.ItemId,
        CreatedAt = label.CreatedAt,
        PrintedAt = label.PrintedAt,
    };
}
