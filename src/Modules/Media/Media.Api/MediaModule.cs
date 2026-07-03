using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Infrastructure.Data;
using LankaConnect.Modules.Media.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.Media.Api;

/// <summary>
/// Composition-root DI extension for the Media module. Hosts in Phase A
/// (single-deployable Host.AllInOne) call this from
/// <c>LankaConnect.API/Program.cs</c> after AppDbContext is registered.
/// Mirrors the W3.4 <see cref="LankaConnect.Modules.Notifications.Api.NotificationsModule"/>
/// composition pattern.
/// </summary>
public static class MediaModule
{
    /// <summary>Per-context migrations history table name (EF convention).</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    /// <summary>
    /// Registers the Media module composition: <see cref="MediaDbContext"/>
    /// + <see cref="IPhotoAlbumRepository"/>. Safe to call once per host.
    /// </summary>
    public static IServiceCollection AddMediaModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to register MediaDbContext.");

        services.AddDbContext<MediaDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(MediaDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, MediaDbContext.SchemaName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
        }, ServiceLifetime.Scoped);

        services.AddScoped<IPhotoAlbumRepository, PhotoAlbumRepository>();

        // Wave 6.5.b canary: wire per-module outbox (producer scoped +
        // OutboxProcessor hosted). The IIntegrationEventOutbox<MediaDbContext>
        // adapter is registered by the composition root (LankaConnect.API) via
        // LankaConnect.Infrastructure — it depends on both this project and the
        // legacy Infrastructure adapter, so it lives up the graph rather than
        // in this per-module extension.
        services.AddModuleOutbox<MediaDbContext>();

        return services;
    }
}
