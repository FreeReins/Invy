using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Managers;

/// <content>The getting-started card: the kind's steps, worked out from what the space has, never stored.</content>
internal sealed partial class SpaceManager
{
    public async Task<GettingStartedResponse> GetGettingStartedAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken) =>
        await GettingStartedAsync(await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken), userId, cancellationToken);

    public async Task<GettingStartedResponse> DismissGettingStartedAsync(
        Guid userId, Guid spaceId, bool dismissed, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        space.Members.First(member => member.UserId == userId).GettingStartedDismissed = dismissed;
        await spaces.SaveAsync(space, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await GettingStartedAsync(space, userId, cancellationToken);
    }

    private async Task<GettingStartedResponse> GettingStartedAsync(Space space, Guid userId, CancellationToken cancellationToken)
    {
        // A kind whose pack has gone since has no steps. A step of a feature that is off is left off.
        var keys = starterPacks.All().FirstOrDefault(pack => pack.Key == space.Preset)?.GettingStarted ?? [];
        var steps = keys.Select(GettingStartedSteps.Find).OfType<GettingStartedSteps.Step>()
            .Where(step => step.Feature is null || space.Features.Contains(step.Feature))
            .ToList();
        var progress = await items.ProgressAsync(space.Id, cancellationToken);
        var done = new Dictionary<string, bool>
        {
            [GettingStartedSteps.Places] = progress.HasPlaces,
            [GettingStartedSteps.Pack] = progress.HasPutAway,
            [GettingStartedSteps.Values] = progress.HasValues,
            [GettingStartedSteps.Audit] = progress.HasSeen,
            [GettingStartedSteps.Invite] = space.Members.Count > 1,
            [GettingStartedSteps.Stock] = progress.HasLowStockLevels,
            [GettingStartedSteps.Lend] = progress.HasLent,
            [GettingStartedSteps.Destination] = progress.HasDestinations,
        };
        if (steps.Any(step => step.Key == GettingStartedSteps.Project))
        {
            done[GettingStartedSteps.Project] = await projects.AnyAsync(space.Id, cancellationToken);
        }

        if (steps.Any(step => step.Key == GettingStartedSteps.Remind))
        {
            var fields = (await types.ListAsync(space.Id, cancellationToken)).SelectMany(type => type.Fields)
                .Where(field => field.Remind is not null).Select(field => field.Id).ToList();
            done[GettingStartedSteps.Remind] = (await items.ListDatesAsync(space.Id, fields, DateOnly.MinValue, DateOnly.MaxValue, 1, cancellationToken)).Count > 0;
        }

        if (steps.Any(step => step.Key == GettingStartedSteps.Labels))
        {
            done[GettingStartedSteps.Labels] = (await labels.ListAsync(space.Id, false, null, 1, cancellationToken)).Count > 0;
        }

        if (steps.Any(step => step.Key == GettingStartedSteps.Photo))
        {
            done[GettingStartedSteps.Photo] = await attachments.AnyPhotoAsync(space.Id, cancellationToken);
        }

        return new GettingStartedResponse(
            steps.Select(step => new GettingStartedStep(step.Key, step.Title, step.Description, done.GetValueOrDefault(step.Key))).ToList(),
            progress.ExamplesLeft,
            space.Members.First(member => member.UserId == userId).GettingStartedDismissed);
    }
}
