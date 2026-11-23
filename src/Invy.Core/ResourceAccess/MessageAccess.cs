using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Invy.Core.ResourceAccess;

/// <summary>
/// Saves the database changes and sends the queued messages together, through Wolverine's EF Core outbox: the
/// messages are written to the database in the same transaction and sent once it commits.
/// </summary>
internal sealed class MessageAccess(IDbContextOutbox<InvyDbContext> outbox, InvyDbContext db) : IMessageAccess
{
    public Task PublishAsync(object message) => outbox.PublishAsync(message).AsTask();

    public Task ScheduleAsync(object message, DateTimeOffset at) => outbox.ScheduleAsync(message, at).AsTask();

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

            // An accessor that ran a statement straight away (ItemAccess.MoveContentsAsync) opened the transaction
            // itself. If saving left it open, it ends here, with everything else.
            if (db.Database.CurrentTransaction is { } transaction)
            {
                await transaction.CommitAsync(cancellationToken);
                await transaction.DisposeAsync();
            }
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException("Someone else changed this while you were working on it. Reload to see their change, then try again.", exception);
        }
    }
}
