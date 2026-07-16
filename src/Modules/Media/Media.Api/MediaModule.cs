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

        // Wave 8.5.f continuation (2026-07-16, Consult #28 R1): per-module
        // SaveChangesInterceptor dispatches domain events raised on PhotoAlbum
        // aggregates that would otherwise be dropped when handlers use
        // IMultiContextUnitOfWork.CommitAsync(new DbContext[] { _mediaContext }, ct)
        // — that path calls _mediaContext.SaveChangesAsync directly (UnitOfWork.cs:106)
        // which bypasses AppDbContext.CommitAsync dispatch. Unblocks PhotoAlbums
        // Wave 9 dispatch-gap fails (9 handlers under Commands/PhotoAlbums/*).
        // Mirrors commit 1212d994 (LankaEvents/Identity/Communications wiring).
        services.AddScoped<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>();

        services.AddDbContext<MediaDbContext>((sp, options) =>
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
            options.AddInterceptors(sp.GetRequiredService<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>());
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
