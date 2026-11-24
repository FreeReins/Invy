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
        Attachments.IsExample = id => Items.Rows.Any(item => item.Id == id && item.IsExample);
    }

    public FixedClock Clock { get; } = new();

    public FakeAccounts Accounts { get; } = new();

    public FakeInbox Inbox { get; } = new();

    public FakeSpaces Spaces { get; } = new();

    public FakeTypes Types { get; } = new();

    public FakeItems Items { get; } = new();

    public FakeLabels Labels { get; } = new();

    public FakeAttachments Attachments { get; } = new();

    public FakeFiles Files { get; } = new();

    public FakeProducts Products { get; } = new();

    public FakeWebhook Webhook { get; } = new();

    public FakeSettings Settings { get; } = new();

    public FakeAi Ai { get; } = new();

    public FakeMessages Messages { get; } = new();

    public AccountManager AccountManager() => Make<AccountManager>();

    public MaintenanceManager MaintenanceManager() => Make<MaintenanceManager>();

    public SpaceManager SpaceManager() => Make<SpaceManager>();

    public TypeManager TypeManager() => Make<TypeManager>();

    public ItemManager ItemManager() => Make<ItemManager>();

    public LabelManager LabelManager() => Make<LabelManager>();

    public AttachmentManager AttachmentManager() => Make<AttachmentManager>();

    /// <summary>A new manager each call, as each request gets in the app; the fakes and engines are shared.</summary>
    private T Make<T>()
        where T : notnull => services.Value.GetRequiredService<T>();

    private ServiceProvider Register()
    {
        var services = new ServiceCollection();

        // Fakes for every accessor.
        services.AddSingleton<IAccountAccess>(Accounts);
        services.AddSingleton<IInboxAccess>(Inbox);
        services.AddSingleton<ISpaceAccess>(Spaces);
        services.AddSingleton<ITypeAccess>(Types);
        services.AddSingleton<IItemAccess>(Items);
        services.AddSingleton<ILabelAccess>(Labels);
        services.AddSingleton<IAttachmentAccess>(Attachments);
        services.AddSingleton<IFileStoreAccess>(Files);
        services.AddSingleton<IProductAccess>(Products);
        services.AddSingleton<IStarterPackAccess>(TestData.StarterPacks);
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
        services.AddSingleton<ItemEngine>();
        services.AddSingleton<TimelineEngine>();
        services.AddSingleton<QuickAddEngine>();
        services.AddSingleton<QueryEngine>();
        services.AddSingleton<LabelEngine>();
        services.AddSingleton<ImageEngine>();
        services.AddTransient<ValidationEngine>();

        // The managers under test.
        services.AddTransient<MaintenanceManager>();
        services.AddTransient<AccountManager>();
        services.AddTransient<SpaceManager>();
        services.AddTransient<TypeManager>();
        services.AddTransient<ItemManager>();
        services.AddTransient<LabelManager>();
        services.AddTransient<AttachmentManager>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
