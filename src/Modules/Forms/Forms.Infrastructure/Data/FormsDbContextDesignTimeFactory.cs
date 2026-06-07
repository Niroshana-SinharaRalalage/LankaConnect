using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LankaConnect.Modules.Forms.Infrastructure.Data;

/// <summary>
/// Design-time factory for <see cref="FormsDbContext"/>. Mirrors the W3.5
/// Notifications pattern — placeholder connection string never opened.
/// </summary>
public sealed class FormsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FormsDbContext>
{
    private const string DesignTimePlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=lankaconnect_design_only;Username=design;Password=design";

    public FormsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FormsDbContext>()
            .UseNpgsql(DesignTimePlaceholderConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(FormsDbContext).Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", FormsDbContext.SchemaName);
            })
            .Options;

        return new FormsDbContext(options);
    }
}
