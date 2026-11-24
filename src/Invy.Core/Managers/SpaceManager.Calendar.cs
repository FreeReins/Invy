using Invy.Core.Engines;
using Invy.Core.Engines.Contracts.Messages;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Managers;

/// <content>
/// The space's calendar feed. Calendar apps can't sign in, so its address carries a secret instead: members see the
/// address, and owners turn the feed on, replace its address or turn it off.
/// </content>
internal sealed partial class SpaceManager
{
    /// <summary>The most dates and loans one feed holds.</summary>
    public const int MaxCalendarEvents = 2000;

    public async Task<CalendarFeedResponse> GetCalendarFeedAsync(Guid userId, Guid spaceId, Uri site, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return Feed(space.Id, space.CalendarToken, site);
    }

    public async Task<CalendarFeedResponse> StartCalendarFeedAsync(Guid userId, Guid spaceId, Uri site, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        space.CalendarToken = CalendarEngine.NewToken();
        await spaces.SaveAsync(space, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return Feed(space.Id, space.CalendarToken, site);
    }

    public async Task<CalendarFeedResponse> StopCalendarFeedAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        var space = await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
        space.CalendarToken = null;
        await spaces.SaveAsync(space, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return new CalendarFeedResponse(null);
    }

    public async Task<string> GetCalendarAsync(Guid spaceId, string token, Uri site, CancellationToken cancellationToken)
    {
        var space = await spaces.FindAsync(spaceId, cancellationToken);
        if (space is null || !CalendarEngine.Matches(space, token))
        {
            throw new NotFoundException("There is no calendar at that address. Copy the address again from the space's settings in Invy.");
        }

        var fields = (await types.ListAsync(spaceId, cancellationToken)).SelectMany(type => type.Fields)
            .Where(field => field.Remind is not null).GroupBy(field => field.Id).ToDictionary(group => group.Key, group => group.First());
        var today = clock.Today();
        var found = await items.ListDatesAsync(spaceId, fields.Keys, today.AddDays(-90), today.AddYears(3), MaxCalendarEvents, cancellationToken);
        var loans = await items.ListLentAsync(spaceId, today.AddYears(10), MaxCalendarEvents, cancellationToken);
        return calendar.Write(space, found.Select(date => new DueDate(space, date.Item, fields[date.FieldId], date.On)), loans, site, clock.GetUtcNow());
    }

    private static CalendarFeedResponse Feed(Guid spaceId, string? token, Uri site) =>
        new(token is null ? null : new Uri(site, $"api/spaces/{spaceId}/calendar/{token}.ics").AbsoluteUri);
}
