namespace Invy.Api;

/// <summary>
/// Prepares the database as the app starts (<see cref="ServiceRegistration.PrepareInvyAsync"/>): after the
/// bundled PostgreSQL is up, and before the message bus starts and creates its own tables, because Invy's are
/// only created in a database that has none yet.
/// </summary>
public sealed class InvyStartup(IServiceProvider services) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken) => services.PrepareInvyAsync();

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
