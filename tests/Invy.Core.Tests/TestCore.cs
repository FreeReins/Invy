using Invy.Core.Engines;
using Invy.Core.Managers;
using Invy.Core.ResourceAccess.Contracts.Services;
using Invy.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Invy.Core.Tests;

/// <summary>
/// Managers built on the fakes above, with the real engines. They are made by dependency injection, so a manager
/// that takes a new accessor or engine needs only that registered here, not a change to every call.
/// </summary>
internal sealed class TestCore
{
    private readonly Lazy<ServiceProvider> services;

    public TestCore()
    {
        services = new(Register);
    }

    public FixedClock Clock { get; } = new();

    public FakeAccounts Accounts { get; } = new();

    public FakeInbox Inbox { get; } = new();

    public FakeWebhook Webhook { get; } = new();

    public FakeSettings Settings { get; } = new();

    public FakeAi Ai { get; } = new();

    public FakeMessages Messages { get; } = new();

    public AccountManager AccountManager() => Make<AccountManager>();

    public MaintenanceManager MaintenanceManager() => Make<MaintenanceManager>();

    /// <summary>A new manager each call, as each request gets in the app; the fakes and engines are shared.</summary>
    private T Make<T>()
        where T : notnull => services.Value.GetRequiredService<T>();

    private ServiceProvider Register()
    {
        var services = new ServiceCollection();

        // Fakes for every accessor.
        services.AddSingleton<IAccountAccess>(Accounts);
        services.AddSingleton<IInboxAccess>(Inbox);
        services.AddSingleton<INotificationAccess>(Webhook);
        services.AddSingleton<ISettingsAccess>(Settings);
        services.AddSingleton<IAiAccess>(Ai);
        services.AddSingleton<IMessageAccess>(Messages);
        services.AddSingleton<IBackupAccess>(new NoBackups());

        // Utilities and the real engines.
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IPasswordHasher>(new PlainHasher());
        services.AddSingleton(TestData.Notifications);
        services.AddSingleton<SecurityEngine>();
        services.AddTransient<ValidationEngine>();

        // The managers under test.
        services.AddTransient<MaintenanceManager>();
        services.AddTransient<AccountManager>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
