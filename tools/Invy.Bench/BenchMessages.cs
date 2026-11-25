using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;

namespace Invy.Bench;

/// <summary>
/// Stands in for <c>MessageAccess</c> without Wolverine: commits the request's changes the same way (one save, then
/// the transaction an accessor opened) and keeps what was published in <see cref="BenchOutbox"/>.
/// </summary>
internal sealed class BenchMessages(InvyDbContext db, BenchOutbox outbox) : IMessageAccess
{
    public Task PublishAsync(object message)
    {
        outbox.Add(message);
        return Task.CompletedTask;
    }

    public Task ScheduleAsync(object message, DateTimeOffset at) => PublishAsync(message);

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        if (db.Database.CurrentTransaction is { } transaction)
        {
            await transaction.CommitAsync(cancellationToken);
            await transaction.DisposeAsync();
        }
    }
}
