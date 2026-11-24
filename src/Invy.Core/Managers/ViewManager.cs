using Invy.Core.Engines;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="IViewManager"/>
internal sealed class ViewManager(
    IViewAccess views,
    IItemAccess items,
    IAccountAccess accounts,
    IAttachmentAccess attachments,
    ITypeAccess types,
    IMessageAccess messages,
    ValidationEngine validation,
    QueryEngine queryEngine,
    TimeProvider clock) : IViewManager
{
    public async Task<IReadOnlyList<ViewResponse>> ListViewsAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        var role = await RoleAsync(spaceId, userId, cancellationToken);
        return (await views.ListAsync(spaceId, userId, cancellationToken)).Select(view => view.ToResponse(userId, role)).ToList();
    }

    public async Task<ViewResponse> CreateViewAsync(Guid userId, Guid spaceId, ViewRequest request, CancellationToken cancellationToken)
    {
        var role = await RoleAsync(spaceId, userId, cancellationToken);
        if (await views.CountOwnedAsync(spaceId, userId, cancellationToken) >= ValidationEngine.ViewsPerPerson)
        {
            throw new RuleViolationException(
                $"You have {ValidationEngine.ViewsPerPerson} views in this space, the most one person can have. Remove one you no longer use first.");
        }

        var now = clock.GetUtcNow();
        var view = new View { SpaceId = spaceId, OwnerId = userId, CreatedAt = now };
        await FillAsync(view, request, role, now, cancellationToken);
        await views.SaveAsync(view, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return view.ToResponse(userId, role);
    }

    public async Task<ViewResponse> UpdateViewAsync(Guid userId, Guid spaceId, Guid id, ViewRequest request, CancellationToken cancellationToken)
    {
        var role = await RoleAsync(spaceId, userId, cancellationToken);
        var view = await ChangeableAsync(spaceId, userId, id, role, cancellationToken);
        await FillAsync(view, request, role, clock.GetUtcNow(), cancellationToken);
        await views.SaveAsync(view, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return view.ToResponse(userId, role);
    }

    public async Task RemoveViewAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        var role = await RoleAsync(spaceId, userId, cancellationToken);
        await ChangeableAsync(spaceId, userId, id, role, cancellationToken);
        await views.RemoveAsync(spaceId, id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<SearchResponse> RunViewAsync(Guid userId, Guid spaceId, Guid id, string? after, CancellationToken cancellationToken)
    {
        var query = await QueryAsync(userId, spaceId, id, cancellationToken);
        return await SearchPages.RunAsync(items, spaceId, query, after, attachments, cancellationToken);
    }

    public async Task<ActivityResponse> ViewTimelineAsync(Guid userId, Guid spaceId, Guid id, string? before, CancellationToken cancellationToken)
    {
        var query = await QueryAsync(userId, spaceId, id, cancellationToken);
        var found = await items.ListMatchingEventsAsync(spaceId, query, ValidationEngine.EventCursor(before), EventPages.PageSize + 1, cancellationToken);
        return await EventPages.ToPageAsync(items, accounts, spaceId, found, cancellationToken);
    }

    /// <summary>The view's search, read again as of now, once the person may see the view.</summary>
    private async Task<ItemQuery> QueryAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var view = await validation.ViewAsync(spaceId, userId, id, cancellationToken);
        return queryEngine.Build(view.Query, await types.ListAsync(spaceId, cancellationToken), clock.GetUtcNow(), view.Sort);
    }

    private async Task<SpaceRole> RoleAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return space.Members.First(member => member.UserId == userId).Role;
    }

    private async Task<View> ChangeableAsync(Guid spaceId, Guid userId, Guid id, SpaceRole role, CancellationToken cancellationToken)
    {
        var view = await validation.ViewAsync(spaceId, userId, id, cancellationToken);
        return ValidationEngine.CanChangeView(view, userId, role)
            ? view
            : throw new NotAllowedException($"Only editors and owners can change shared views. Save a copy of {view.Name} as your own instead.");
    }

    /// <summary>Copies the request onto the view once every part of it reads.</summary>
    private async Task FillAsync(View view, ViewRequest request, SpaceRole role, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (request.Shared && role < SpaceRole.Editor)
        {
            throw new NotAllowedException("Only editors and owners can share views. Keep it to yourself, or ask an owner to make you an editor.");
        }

        var spaceTypes = await types.ListAsync(view.SpaceId, cancellationToken);
        var query = (request.Query ?? string.Empty).Trim();
        var sort = string.IsNullOrWhiteSpace(request.Sort) ? null : request.Sort.Trim().ToLowerInvariant();
        queryEngine.Build(query, spaceTypes, now, sort);
        var display = ValidationEngine.ViewDisplay(request.Display);

        view.Name = ValidationEngine.ViewName(request.Name);
        view.Query = query;
        view.Display = display;
        view.Columns = ValidationEngine.ViewColumns(request.Columns, spaceTypes);
        view.Sort = sort;
        view.GroupBy = ValidationEngine.ViewGroupBy(display, request.GroupBy, spaceTypes);
        view.Shared = request.Shared;
        view.Pinned = request.Pinned;
        view.UpdatedAt = now;
    }
}
