using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// Views: saved searches with how to show them, kept by one person or shared with the space. Pinned views show on
/// Find. Any member can make views for themselves; editors and owners share them and change shared ones.
/// </summary>
public interface IViewManager
{
    /// <summary>The space's shared views and the person's own, pinned first, then by name.</summary>
    Task<IReadOnlyList<ViewResponse>> ListViewsAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>Saves a new view. Its search is read now, so a mistake in it is reported before it is saved.</summary>
    /// <exception cref="RuleViolationException">A name, search, column or group breaks a rule, or the person has too many views.</exception>
    /// <exception cref="NotAllowedException">A viewer asked to share it.</exception>
    Task<ViewResponse> CreateViewAsync(Guid userId, Guid spaceId, ViewRequest request, CancellationToken cancellationToken);

    /// <summary>Changes a view: its maker can change a personal one, editors and owners a shared one.</summary>
    /// <exception cref="RuleViolationException">A name, search, column or group breaks a rule.</exception>
    /// <exception cref="NotAllowedException">The person can't change it, or a viewer asked to share it.</exception>
    Task<ViewResponse> UpdateViewAsync(Guid userId, Guid spaceId, Guid id, ViewRequest request, CancellationToken cancellationToken);

    /// <summary>Removes a view, as <see cref="UpdateViewAsync"/> allows. The items it found are untouched.</summary>
    Task RemoveViewAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a view's search as of now, 50 at a time, as <see cref="IItemManager.SearchAsync"/> would with its search
    /// and sort. <paramref name="after"/> is the last page's Next.
    /// </summary>
    /// <exception cref="RuleViolationException">Its search no longer reads, such as a field that was removed; the message says which term.</exception>
    Task<SearchResponse> RunViewAsync(Guid userId, Guid spaceId, Guid id, string? after, CancellationToken cancellationToken);

    /// <summary>
    /// The events of the items a view finds as of now, newest first, 50 at a time, for its Timeline display.
    /// <paramref name="before"/> is the last page's Next.
    /// </summary>
    /// <exception cref="RuleViolationException">Its search no longer reads; the message says which term.</exception>
    Task<ActivityResponse> ViewTimelineAsync(Guid userId, Guid spaceId, Guid id, string? before, CancellationToken cancellationToken);
}
