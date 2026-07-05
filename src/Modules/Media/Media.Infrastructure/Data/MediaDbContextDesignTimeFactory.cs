using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace LankaConnect.Modules.Media.Infrastructure.Data;

/// <summary>
/// Design-time factory for <see cref="MediaDbContext"/>. Used by
/// <c>dotnet ef</c> CLI commands (e.g. <c>migrations add</c>) to materialise
/// the DbContext without booting the host. Mirrors the W3.5 Notifications
/// pattern. The placeholder connection string is never opened — EF's
/// design-time CLI only inspects the model.
/// </summary>
public sealed class MediaDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    private const string DesignTimePlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=lankaconnect_design_only;Username=design;Password=design";

    public MediaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(DesignTimePlaceholderConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(MediaDbContext).Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MediaDbContext.SchemaName);
            })
            .Options;

        return new MediaDbContext(options);
    }
}
