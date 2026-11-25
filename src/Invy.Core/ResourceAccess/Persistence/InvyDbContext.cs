using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Invy.Core.ResourceAccess.Persistence;

/// <summary>The PostgreSQL database: accounts, sign-in links, API tokens, inboxes, AI models, settings, spaces, items and labels.</summary>
internal sealed class InvyDbContext(DbContextOptions<InvyDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<AccessLinkEntity> AccessLinks => Set<AccessLinkEntity>();

    public DbSet<ApiTokenEntity> ApiTokens => Set<ApiTokenEntity>();

    public DbSet<AiModelEntity> AiModels => Set<AiModelEntity>();

    public DbSet<AiRouteEntity> AiRoutes => Set<AiRouteEntity>();

    public DbSet<InboxEntryEntity> InboxEntries => Set<InboxEntryEntity>();

    public DbSet<SpaceEntity> Spaces => Set<SpaceEntity>();

    public DbSet<ItemTypeEntity> ItemTypes => Set<ItemTypeEntity>();

    public DbSet<ItemEntity> Items => Set<ItemEntity>();

    public DbSet<ItemEventEntity> ItemEvents => Set<ItemEventEntity>();

    public DbSet<ItemPathEntity> ItemPaths => Set<ItemPathEntity>();

    public DbSet<ItemValueEntity> ItemValues => Set<ItemValueEntity>();

    public DbSet<LabelEntity> Labels => Set<LabelEntity>();

    public DbSet<AttachmentEntity> Attachments => Set<AttachmentEntity>();

    public DbSet<ViewEntity> Views => Set<ViewEntity>();

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();

    public DbSet<ImportEntity> Imports => Set<ImportEntity>();

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

        // A space's members go with it, and an account's memberships go with the account.
        modelBuilder.Entity<SpaceEntity>(space =>
        {
            space.ToTable("Spaces");
            space.HasMany(s => s.Members).WithOne().HasForeignKey(m => m.SpaceId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SpaceMemberEntity>(member =>
        {
            member.ToTable("SpaceMembers");
            member.HasKey(m => new { m.SpaceId, m.UserId });
            member.HasIndex(m => m.UserId);
            member.HasOne<UserEntity>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // A space's types go with it. Version makes a save from an older copy fail instead of overwriting.
        modelBuilder.Entity<ItemTypeEntity>(type =>
        {
            type.ToTable("ItemTypes");
            type.HasIndex(t => t.SpaceId);
            type.HasOne<SpaceEntity>().WithMany().HasForeignKey(t => t.SpaceId).OnDelete(DeleteBehavior.Cascade);
            type.Property(t => t.Fields).HasColumnType("jsonb");
            type.Property(t => t.Version).IsConcurrencyToken();
        });

        // Items: a space's items go with it, and a removed type leaves its items without one. A container can't be
        // deleted while something is in it (NO ACTION is checked at the end of the statement, so clearing the whole
        // table for a restore still works). Path is an ltree of the ids of the item's containers and itself.
        modelBuilder.Entity<ItemEntity>(item =>
        {
            item.ToTable("Items");
            item.HasOne<SpaceEntity>().WithMany().HasForeignKey(i => i.SpaceId).OnDelete(DeleteBehavior.Cascade);
            item.HasOne<ItemTypeEntity>().WithMany().HasForeignKey(i => i.TypeId).OnDelete(DeleteBehavior.SetNull);
            item.HasOne<ItemEntity>().WithMany().HasForeignKey(i => i.ParentId).OnDelete(DeleteBehavior.NoAction);
            item.Property(i => i.Fields).HasColumnType("jsonb");
            item.Property(i => i.Version).IsConcurrencyToken();
            item.HasIndex(i => new { i.SpaceId, i.ParentId, i.Name });

            // Phase 7: the path and depth live in "ItemPaths" (ItemPathEntity), loaded with every item, so moving a
            // container rewrites those narrow rows and their one index, not the items inside. Path and Depth here only
            // pass through to it.
            item.Ignore(i => i.Path);
            item.Ignore(i => i.Depth);
            item.HasOne(i => i.Place).WithOne().HasForeignKey<ItemPathEntity>(p => p.Id).OnDelete(DeleteBehavior.Cascade);
            item.Navigation(i => i.Place).IsRequired().AutoInclude();

            // Search (docs/developers/design.md, Search). The "Search" tsvector column is generated by PostgreSQL from
            // the name, marking, tags, text fields and notes; the AddSearch migration adds it and its GIN index, and the
            // model leaves it out so EF never writes it.
            item.HasIndex(i => new { i.SpaceId, i.Name, i.Id }, "IX_Items_SpaceId_Name_Id");
            item.HasIndex(i => new { i.SpaceId, i.TypeId }, "IX_Items_SpaceId_TypeId");
            item.HasIndex(i => new { i.SpaceId, i.LastSeenAt }, "IX_Items_SpaceId_LastSeenAt");
            item.HasIndex(i => new { i.SpaceId, i.CreatedAt }, "IX_Items_SpaceId_CreatedAt");
            item.HasIndex(i => new { i.SpaceId, i.UpdatedAt, i.Id }, "IX_Items_SpaceId_UpdatedAt_Id");
            item.HasIndex(i => i.Name, "IX_Items_Name_Trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
            item.HasIndex(i => i.Marking, "IX_Items_Marking_Trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
            item.HasIndex(i => i.Fields, "IX_Items_Fields").HasMethod("gin").HasOperators("jsonb_path_ops");
            item.HasIndex(i => i.Tags, "IX_Items_Tags").HasMethod("gin");
            item.HasIndex(i => i.SpaceId, "IX_Items_Containers").HasFilter("\"HoldsItems\"");
            item.HasIndex(i => i.SpaceId, "IX_Items_Counted").HasFilter("\"Quantity\" IS NOT NULL");
            item.HasIndex(i => i.SpaceId, "IX_Items_Examples").HasFilter("\"IsExample\"");
            item.HasIndex(i => new { i.SpaceId, i.Status }, "IX_Items_NotActive").HasFilter("\"Status\" <> 'Active'");
            // Phase 6: stock levels, loans and moving destinations, each with a small filtered index for its lists.
            item.HasOne<ItemEntity>().WithMany().HasForeignKey(i => i.DestinationId).OnDelete(DeleteBehavior.SetNull);
            item.Property(i => i.LentTo).HasMaxLength(100);
            item.HasIndex(i => i.SpaceId, "IX_Items_LowStock").HasFilter("\"LowStockAt\" IS NOT NULL");
            item.HasIndex(i => new { i.SpaceId, i.DueOn }, "IX_Items_Lent").HasFilter("\"LentAt\" IS NOT NULL");
            item.HasIndex(i => new { i.DestinationId, i.SpaceId }, "IX_Items_Destination").HasFilter("\"DestinationId\" IS NOT NULL");
            item.Property(i => i.LabelCode).HasMaxLength(6);
            item.HasIndex(i => i.LabelCode).IsUnique().HasFilter("\"LabelCode\" IS NOT NULL");
        });

        // The timeline: append-only. Indexed for an item's own events, what went in and out of a container, and the
        // space's feed. BRIN on At keeps a very long table cheap to scan by time.
        modelBuilder.Entity<ItemEventEntity>(itemEvent =>
        {
            itemEvent.ToTable("ItemEvents");
            itemEvent.HasOne<SpaceEntity>().WithMany().HasForeignKey(e => e.SpaceId).OnDelete(DeleteBehavior.Cascade);
            itemEvent.HasOne<ItemEntity>().WithMany().HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            itemEvent.Property(e => e.Data).HasColumnType("jsonb");
            itemEvent.HasIndex(e => new { e.ItemId, e.At });
            itemEvent.HasIndex(e => new { e.ToParent, e.At });
            itemEvent.HasIndex(e => new { e.FromParent, e.At });
            itemEvent.HasIndex(e => new { e.SpaceId, e.At });
            itemEvent.HasIndex(e => e.At).HasMethod("brin").HasDatabaseName("IX_ItemEvents_At_Brin");
        });

        modelBuilder.Entity<ItemPathEntity>(path =>
        {
            path.ToTable("ItemPaths");
            path.Property(p => p.Path).HasColumnType("ltree");
            path.HasIndex(p => p.Path).HasMethod("gist");
        });

        // Sortable field values, one row per item and field, rewritten with the item.
        modelBuilder.Entity<ItemValueEntity>(value =>
        {
            value.ToTable("ItemValues");
            value.HasKey(v => new { v.ItemId, v.FieldId });
            value.HasOne<ItemEntity>().WithMany().HasForeignKey(v => v.ItemId).OnDelete(DeleteBehavior.Cascade);
            value.HasIndex(v => new { v.FieldId, v.Num });
            value.HasIndex(v => new { v.FieldId, v.Date });
            value.HasIndex(v => new { v.FieldId, v.Text });
        });

        // QR labels. The code is the key, unique across the install, so a scan finds its space. A label goes with its
        // space; removing its item frees it. Items keep a copy of their code (LabelCode) for search.
        modelBuilder.Entity<LabelEntity>(label =>
        {
            label.ToTable("Labels");
            label.HasKey(l => l.Code);
            label.Property(l => l.Code).HasMaxLength(6);
            label.HasOne<SpaceEntity>().WithMany().HasForeignKey(l => l.SpaceId).OnDelete(DeleteBehavior.Cascade);
            label.HasOne<ItemEntity>().WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.SetNull);
            label.HasIndex(l => new { l.SpaceId, l.Code });
            label.HasIndex(l => l.ItemId);
        });

        // Files attached to items. The bytes are on disk under Data:Root/files, named by their SHA-256; the row goes
        // with its item and its space.
        modelBuilder.Entity<AttachmentEntity>(attachment =>
        {
            attachment.ToTable("Attachments");
            attachment.HasOne<SpaceEntity>().WithMany().HasForeignKey(a => a.SpaceId).OnDelete(DeleteBehavior.Cascade);
            attachment.HasOne<ItemEntity>().WithMany().HasForeignKey(a => a.ItemId).OnDelete(DeleteBehavior.Cascade);
            attachment.Property(a => a.Name).HasMaxLength(200);
            attachment.Property(a => a.ContentType).HasMaxLength(100);
            attachment.Property(a => a.Path).HasMaxLength(200);
            attachment.Property(a => a.Sha256).HasMaxLength(64);
            attachment.HasIndex(a => new { a.SpaceId, a.ItemId, a.CreatedAt });
            attachment.HasIndex(a => new { a.SpaceId, a.Path });
        });

        // Saved views. A view goes with its space and with the person who made it.
        modelBuilder.Entity<ViewEntity>(view =>
        {
            view.ToTable("Views");
            view.HasOne<SpaceEntity>().WithMany().HasForeignKey(v => v.SpaceId).OnDelete(DeleteBehavior.Cascade);
            view.HasOne<UserEntity>().WithMany().HasForeignKey(v => v.OwnerId).OnDelete(DeleteBehavior.Cascade);
            view.Property(v => v.Name).HasMaxLength(80);
            view.Property(v => v.Query).HasMaxLength(500);
            view.Property(v => v.Sort).HasMaxLength(20);
            view.Property(v => v.GroupBy).HasMaxLength(40);
            view.HasIndex(v => new { v.SpaceId, v.OwnerId });
        });

        // Projects, with what each sets aside. A reservation goes with its project or its item.
        modelBuilder.Entity<ProjectEntity>(project =>
        {
            project.ToTable("Projects");
            project.HasOne<SpaceEntity>().WithMany().HasForeignKey(p => p.SpaceId).OnDelete(DeleteBehavior.Cascade);
            project.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.Cascade);
            project.Property(p => p.Name).HasMaxLength(100);
            project.Property(p => p.Version).IsConcurrencyToken();
            project.HasIndex(p => new { p.SpaceId, p.Status, p.Name });
        });
        modelBuilder.Entity<ProjectItemEntity>(reserved =>
        {
            reserved.ToTable("ProjectItems");
            reserved.HasKey(r => new { r.ProjectId, r.ItemId });
            reserved.HasOne<ItemEntity>().WithMany().HasForeignKey(r => r.ItemId).OnDelete(DeleteBehavior.Cascade);
            reserved.HasIndex(r => r.ItemId);
        });

        // Imports: short-lived work, so not in backups. The file is kept under the space's files until it finishes.
        modelBuilder.Entity<ImportEntity>(import =>
        {
            import.ToTable("Imports");
            import.HasOne<SpaceEntity>().WithMany().HasForeignKey(i => i.SpaceId).OnDelete(DeleteBehavior.Cascade);
            import.Property(i => i.Mapping).HasColumnType("jsonb");
            import.Property(i => i.Problems).HasColumnType("jsonb");
            import.Property(i => i.FileName).HasMaxLength(200);
            import.Property(i => i.Version).IsConcurrencyToken();
            import.HasIndex(i => new { i.SpaceId, i.CreatedAt });
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
