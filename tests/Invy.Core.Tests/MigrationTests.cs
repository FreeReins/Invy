using Invy.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invy.Core.Tests;

/// <summary>Checks the migrations against the model, without a database.</summary>
public class MigrationTests
{
    private static InvyDbContext Context() =>
        new(new DbContextOptionsBuilder<InvyDbContext>().UseNpgsql("Host=unused").Options);

    [Test]
    public async Task Every_model_change_has_a_migration()
    {
        using var db = Context();
        await Assert.That(db.Database.HasPendingModelChanges()).IsFalse()
            .Because("an entity changed without a migration: run `dotnet ef migrations add <Name> --project src/Invy.Core "
                + "--output-dir ResourceAccess/Persistence/Migrations` (AGENTS.md)");
    }

    [Test]
    public async Task The_baseline_is_the_first_migration()
    {
        using var db = Context();
        await Assert.That(db.Database.GetMigrations().First()).IsEqualTo(DatabaseUpgrade.BaselineMigration);
    }
}
