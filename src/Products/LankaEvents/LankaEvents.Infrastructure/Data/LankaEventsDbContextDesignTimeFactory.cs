using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Data;

/// <summary>
/// Design-time factory for <see cref="LankaEventsDbContext"/>. Used by
/// <c>dotnet ef</c> CLI commands (e.g. <c>migrations add</c>) to materialise
/// the DbContext without booting the host — the host's <c>Program.cs</c>
/// requires a live Npgsql connection string and would otherwise throw at
/// design time on a developer machine that doesn't have one configured.
/// </summary>
/// <remarks>
/// The connection string here is a placeholder used ONLY for EF model
/// inspection / migration scaffolding. It is NEVER opened — EF's design-time
/// CLI never executes against it. Pass a real connection string with
/// <c>--connection</c> when actually applying migrations.
/// </remarks>
public sealed class LankaEventsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LankaEventsDbContext>
{
    private const string DesignTimePlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=lankaconnect_design_only;Username=design;Password=design";

    public LankaEventsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LankaEventsDbContext>()
            .UseNpgsql(DesignTimePlaceholderConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(LankaEventsDbContext).Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", LankaEventsDbContext.SchemaName);
            })
            .Options;

        return new LankaEventsDbContext(options);
    }
}
