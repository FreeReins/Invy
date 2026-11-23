using System.Net.Sockets;
using Invy.Core.Engines;
using Invy.Core.Engines.Transformation;
using Invy.Core.Managers;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.ResourceAccess.Persistence;
using Invy.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

namespace Invy.Core;

/// <summary>
/// Wires up everything behind the manager interfaces in <see cref="Managers.Contracts.Services"/>: managers, engines,
/// resource accessors and utilities, the PostgreSQL database and the message bus's storage in it.
/// </summary>
public static class CoreRegistration
{
    /// <summary>The schema that holds the message bus's own tables, next to Invy's.</summary>
    public const string MessagingSchema = "wolverine";

    /// <summary>How long startup waits for PostgreSQL to accept connections before giving up.</summary>
    private static readonly TimeSpan DatabaseStartupWait = TimeSpan.FromSeconds(60);

    public static IServiceCollection AddInvyCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings and reference data: loaded once, never changed while running.
        services.TryAddSingleton(TimeProvider.System);

        // Utilities.
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IStarterPackAccess>(new StarterPackAccess(DataPath(configuration, "Data:StarterPacks", "config")));

        // Resource access: PostgreSQL, the webhook, the AI models and the backup folder.
        services.AddDbContext<InvyDbContext>(options => options.UseNpgsql(ConnectionString(configuration)));
        services.AddScoped<IAccountAccess, AccountAccess>();
        services.AddScoped<IInboxAccess, InboxAccess>();
        services.AddScoped<ISpaceAccess, SpaceAccess>();
        services.AddScoped<ITypeAccess, TypeAccess>();
        services.AddScoped<IItemAccess, ItemAccess>();
        services.AddScoped<ILabelAccess, LabelAccess>();
        services.AddScoped<ISettingsAccess, SettingsAccess>();
        services.AddScoped<IMessageAccess, MessageAccess>();
        var backupFolder = StoragePath(configuration, "Data:Backups", "backups");
        services.AddScoped<IBackupAccess>(provider => new BackupAccess(provider.GetRequiredService<InvyDbContext>(), backupFolder));
        services.AddHttpClient<INotificationAccess, NotificationAccess>(client => client.Timeout = TimeSpan.FromSeconds(10));
        // AI models are set up by an admin and may run on the home network (Ollama, LM Studio), so private addresses
        // are allowed. Reading a photo can take a minute or more.
        services.AddHttpClient<IAiAccess, AiAccess>(client => client.Timeout = TimeSpan.FromSeconds(180));

        // Engines. They hold no state; ValidationEngine reads through the accessors, so it lives per request.
        services.AddSingleton<TransformationEngine>();
        services.AddSingleton<SecurityEngine>();
        services.AddSingleton<NotificationEngine>();
        services.AddSingleton<ItemEngine>();
        services.AddSingleton<TimelineEngine>();
        services.AddSingleton<QuickAddEngine>();
        services.AddSingleton<QueryEngine>();
        services.AddSingleton<LabelEngine>();
        services.AddScoped<ValidationEngine>();

        // Managers: one per request or message, sharing one database transaction.
        services.AddScoped<IAccountManager, AccountManager>();
        services.AddScoped<IMaintenanceManager, MaintenanceManager>();
        services.AddScoped<ISpaceManager, SpaceManager>();
        services.AddScoped<ITypeManager, TypeManager>();
        services.AddScoped<IItemManager, ItemManager>();
        services.AddScoped<ILabelManager, LabelManager>();

        return services;
    }

    /// <summary>
    /// Keeps the message bus's messages in PostgreSQL, in the <see cref="MessagingSchema"/> schema, so commands and
    /// events published by the managers are saved in the same transaction as their data and survive a restart.
    /// Their JSON comes from <see cref="ContractJsonContext"/>, made at build time.
    /// </summary>
    public static void UseInvyMessageStorage(this WolverineOptions wolverine, IConfiguration configuration)
    {
        wolverine.UseSystemTextJsonForSerialization(json => json.TypeInfoResolverChain.Insert(0, ContractJsonContext.Default));
        wolverine.PersistMessagesWithPostgresql(ConnectionString(configuration), MessagingSchema);
        wolverine.UseEntityFrameworkCoreTransactions();
        wolverine.Policies.UseDurableLocalQueues();
        wolverine.UseInvyQueues();
        wolverine.UseInvyErrorPolicies();
    }

    /// <summary>
    /// Each handler of an event runs on its own (it can fail, retry or wait without holding up the others), on the
    /// queue named by its <c>[StickyHandler]</c>. Queue routing stays in this one method, so a queue can later move
    /// to another transport without touching the handlers.
    /// </summary>
    private static void UseInvyQueues(this WolverineOptions wolverine)
    {
        wolverine.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
        wolverine.Durability.MessageIdentity = MessageIdentity.IdAndDestination;

        wolverine.LocalQueue(MessageQueues.Maintenance).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Ai).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Images).UseDurableInbox().MaximumParallelMessages(2);
        wolverine.LocalQueue(MessageQueues.Notifications).UseDurableInbox().MaximumParallelMessages(2);
        wolverine.LocalQueue(MessageQueues.Inbox).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Rollups).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Live).BufferedInMemory();
    }

    /// <summary>
    /// Retries come from here, never from handlers. A database that hiccups is retried at once, then later; an
    /// outside service that is down is retried over the next half hour; anything else is a bug, and goes straight to
    /// the dead-letter table (wolverine_dead_letters), where it can be replayed once fixed.
    /// </summary>
    private static void UseInvyErrorPolicies(this WolverineOptions wolverine)
    {
        wolverine.Policies.OnException<NpgsqlException>(exception => exception.IsTransient)
            .Or<TimeoutException>()
            .Or<DbUpdateException>(exception => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            .RetryWithCooldown(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1))
            .Then.ScheduleRetry(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
            .Then.MoveToErrorQueue();

        wolverine.Policies.OnException<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TransientDeliveryException>()
            .Or<AiUnavailableException>(exception => exception.IsTransient)
            .ScheduleRetry(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
            .Then.MoveToErrorQueue();

        wolverine.Policies.OnAnyException().MoveToErrorQueue();
    }

    /// <summary>
    /// Creates Invy's tables on first start and applies any newer migrations on later ones
    /// (<see cref="DatabaseUpgrade"/>). Waits for PostgreSQL to accept connections first, so Invy can start at the
    /// same time as its database. Other errors, such as a wrong password, fail straight away. Run it before the
    /// message bus starts, so no handler meets an old schema.
    /// </summary>
    public static async Task InitializeInvyDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Invy.Database");
        var giveUpAt = DateTimeOffset.UtcNow + DatabaseStartupWait;

        while (true)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<InvyDbContext>();
            var database = context.Database;
            try
            {
                await DatabaseUpgrade.RunAsync(context, logger, cancellationToken);
                return;
            }
            catch (Exception exception) when (IsUnreachable(exception))
            {
                var server = DescribeServer(database.GetConnectionString());
                if (DateTimeOffset.UtcNow >= giveUpAt)
                {
                    throw new InvalidOperationException(
                        $"PostgreSQL at {server} didn't accept connections within {DatabaseStartupWait.TotalSeconds:0} seconds. " +
                        "Check that the database is running and that ConnectionStrings:Invy points at it.", exception);
                }

                logger.LogWarning("Waiting for PostgreSQL at {Server}: {Reason}", server, exception.GetBaseException().Message);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Resolves a path to a file Invy ships with, such as a starter catalog in config/, from settings. Relative paths are
    /// relative to the application folder.
    /// </summary>
    public static string DataPath(IConfiguration configuration, string key, string fallback) =>
        Resolve(AppContext.BaseDirectory, configuration[key] is { Length: > 0 } configured ? configured : fallback);

    /// <summary>
    /// The folder that holds everything Invy writes and must keep: its database, session keys, HTTPS certificate,
    /// backups and settings.json (Data:Root). A relative path is relative to the application folder, which is also the
    /// default; the container image sets /data.
    /// </summary>
    public static string DataRoot(IConfiguration configuration) => DataPath(configuration, "Data:Root", ".");

    /// <summary>Resolves a path to something Invy writes, from settings. Relative paths are relative to <see cref="DataRoot"/>.</summary>
    public static string StoragePath(IConfiguration configuration, string key, string fallback) =>
        Resolve(DataRoot(configuration), configuration[key] is { Length: > 0 } configured ? configured : fallback);

    private static string Resolve(string folder, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(folder, path));

    private static string ConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("Invy")
        ?? throw new InvalidOperationException("Set the ConnectionStrings__Invy setting to a PostgreSQL connection string.");

    /// <summary>True when the database can't be reached yet, as opposed to rejecting what was sent.</summary>
    private static bool IsUnreachable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException or NpgsqlException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Host and port from a connection string, for messages. Never includes the password.</summary>
    private static string DescribeServer(string? connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return $"{builder.Host}:{builder.Port}";
    }
}
