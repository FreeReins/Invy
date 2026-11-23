using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Invy.Core.ResourceAccess.Persistence;

/// <summary>
/// Builds the context for <c>dotnet ef migrations add</c>, which needs the model but never a database. The
/// connection string is a placeholder: nothing connects to it.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InvyDbContext>
{
    public InvyDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<InvyDbContext>().UseNpgsql("Host=localhost;Database=invy").Options);
}
