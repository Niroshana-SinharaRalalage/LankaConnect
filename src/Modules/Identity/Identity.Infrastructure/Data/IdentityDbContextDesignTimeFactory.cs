using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace LankaConnect.Modules.Identity.Infrastructure.Data;

/// <summary>
/// Design-time factory for <see cref="IdentityDbContext"/>. Used by
/// <c>dotnet ef</c> CLI commands (e.g. <c>migrations add</c>) to materialise
/// the DbContext without booting the host. Mirrors the Media/Forms/LankaEvents
/// factories. The placeholder connection string is never opened — EF's
/// design-time CLI only inspects the model.
/// </summary>
public sealed class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    private const string DesignTimePlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=lankaconnect_design_only;Username=design;Password=design";

    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(DesignTimePlaceholderConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.SchemaName);
            })
            .Options;

        return new IdentityDbContext(options);
    }
}
