using Invy.Core.Engines;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Managers;

/// <content>Each person's inbox, the messages behind the bell on the web page.</content>
internal sealed partial class AccountManager
{
    /// <summary>How many messages each person keeps. Older ones are dropped as new ones arrive.</summary>
    internal const int InboxSize = 50;

    public async Task<InboxResponse> GetInboxAsync(Guid userId, CancellationToken cancellationToken) =>
        Describe(await inbox.ListAsync(userId, cancellationToken));

    public async Task<InboxResponse> AddInboxNoteAsync(Guid userId, InboxNoteRequest request, CancellationToken cancellationToken)
    {
        var text = ValidationEngine.InboxNote(request.Text);
        await validation.UserAsync(userId, cancellationToken);
        await AddAsync(userId, text, InboxSource.Page, sourceId: null, cancellationToken);
        await messages.PublishAsync(new InboxChanged(userId));
        await messages.CommitAsync(cancellationToken);
        return await GetInboxAsync(userId, cancellationToken);
    }

    public async Task<InboxResponse> MarkInboxReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        await inbox.MarkReadAsync(userId, clock.GetUtcNow(), cancellationToken);
        await messages.PublishAsync(new InboxChanged(userId));
        await messages.CommitAsync(cancellationToken);
        return await GetInboxAsync(userId, cancellationToken);
    }

    public async Task<InboxResponse> ClearInboxAsync(Guid userId, CancellationToken cancellationToken)
    {
        await inbox.ClearAsync(userId, cancellationToken);
        await messages.PublishAsync(new InboxChanged(userId));
        await messages.CommitAsync(cancellationToken);
        return new InboxResponse([], 0);
    }

    private static InboxResponse Describe(List<InboxEntry> entries) => new(
        entries.Select(entry => new InboxEntryResponse(entry.Id, entry.Text, entry.Source, entry.CreatedAt, entry.ReadAt is not null)).ToList(),
        entries.Count(entry => entry.ReadAt is null));

    /// <summary>Adds an unread message, then drops the oldest past <see cref="InboxSize"/>.</summary>
    private async Task AddAsync(Guid userId, string text, InboxSource source, Guid? sourceId, CancellationToken cancellationToken)
    {
        await inbox.AddAsync(
            new InboxEntry { UserId = userId, Text = text, Source = source, SourceId = sourceId, CreatedAt = clock.GetUtcNow() },
            cancellationToken);
        var dropped = (await inbox.ListAsync(userId, cancellationToken)).Skip(InboxSize).Select(entry => entry.Id).ToList();
        if (dropped.Count > 0)
        {
            await inbox.RemoveAsync(userId, dropped, cancellationToken);
        }
    }
}
