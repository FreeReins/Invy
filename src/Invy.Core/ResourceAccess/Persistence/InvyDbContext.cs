using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Invy.Core.ResourceAccess.Persistence;

/// <summary>The PostgreSQL database: accounts, sign-in links, API tokens, inboxes, AI models and settings.</summary>
internal sealed class InvyDbContext(DbContextOptions<InvyDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<AccessLinkEntity> AccessLinks => Set<AccessLinkEntity>();

    public DbSet<ApiTokenEntity> ApiTokens => Set<ApiTokenEntity>();

    public DbSet<AiModelEntity> AiModels => Set<AiModelEntity>();

    public DbSet<AiRouteEntity> AiRoutes => Set<AiRouteEntity>();

    public DbSet<InboxEntryEntity> InboxEntries => Set<InboxEntryEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 3);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ltree holds each item's place in its containers (path), and pg_trgm indexes text for search.
        // Migrations turn them on with CREATE EXTENSION IF NOT EXISTS.
        modelBuilder.HasPostgresExtension("ltree");
        modelBuilder.HasPostgresExtension("pg_trgm");

        // Table names are given explicitly so they don't follow the entity class names.
        modelBuilder.Entity<UserEntity>(user =>
        {
            user.ToTable("Users");
            user.HasIndex(u => u.Username).IsUnique();
        });
        modelBuilder.Entity<AccessLinkEntity>(link =>
        {
            link.ToTable("AccessLinks");
            link.HasIndex(l => l.TokenHash).IsUnique();
        });
        modelBuilder.Entity<ApiTokenEntity>(token =>
        {
            token.ToTable("ApiTokens");
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<SecuritySettingsEntity>().ToTable("SecuritySettings");
        modelBuilder.Entity<NotificationSettingsEntity>().ToTable("NotificationSettings");
        modelBuilder.Entity<BackupSettingsEntity>().ToTable("BackupSettings");
        modelBuilder.Entity<AiModelEntity>().ToTable("AiModels");
        modelBuilder.Entity<AiRouteEntity>(route =>
        {
            route.ToTable("AiRoutes");
            route.HasKey(r => r.Task);
        });

        modelBuilder.Entity<InboxEntryEntity>(entry =>
        {
            entry.ToTable("InboxEntries");
            entry.HasIndex(e => new { e.UserId, e.CreatedAt });
            entry.HasIndex(e => new { e.UserId, e.SourceId }).IsUnique().HasFilter("\"SourceId\" IS NOT NULL");
        });

        StoreEnumsByName(modelBuilder);
        UseKeysGeneratedInCode(modelBuilder);
    }

    /// <summary>
    /// Stores every enum column by name, so the tables stay readable and survive reordering. Applies to enums added
    /// later too; <c>SchemaTests</c> checks it.
    /// </summary>
    private static void StoreEnumsByName(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            foreach (var property in entityType.GetProperties().ToList())
            {
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
                {
                    modelBuilder.Entity(entityType.ClrType).Property(property.Name).HasConversion<string>().HasMaxLength(20);
                }
            }
        }
    }

    /// <summary>
    /// Ids are created in code (see the data contracts' Id initializers). Telling EF so makes it insert
    /// children added to an already-saved parent, instead of assuming they exist and updating them.
    /// </summary>
    private static void UseKeysGeneratedInCode(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetKeys().SelectMany(key => key.Properties))
            {
                property.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
}
