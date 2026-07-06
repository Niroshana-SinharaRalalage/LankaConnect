using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LankaConnect.Modules.Communications.Infrastructure.Data;

/// <summary>
/// Design-time factory for <see cref="CommunicationsDbContext"/> — mirrors
/// NotificationsDbContextDesignTimeFactory. Used by <c>dotnet ef</c> CLI only.
/// </summary>
public sealed class CommunicationsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CommunicationsDbContext>
{
    private const string DesignTimePlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=lankaconnect_design_only;Username=design;Password=design";

    public CommunicationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CommunicationsDbContext>()
            .UseNpgsql(DesignTimePlaceholderConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(CommunicationsDbContext).Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CommunicationsDbContext.SchemaName);
            })
            .Options;

        return new CommunicationsDbContext(options);
    }
}
