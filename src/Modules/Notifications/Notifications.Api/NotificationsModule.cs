using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Notifications.Domain;
using LankaConnect.Modules.Notifications.Infrastructure.Data;
using LankaConnect.Modules.Notifications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LankaConnect.Modules.Notifications.Api;

/// <summary>
/// Composition-root DI extension for the Notifications module. Hosts in
/// Phase A (single-deployable <c>Host.AllInOne</c>) call this from
/// <c>LankaConnect.API/Program.cs</c> after AppDbContext is registered.
/// In Phase B (per-module deployment), the module's own Host calls this
/// during its startup. Same contract; different composition root.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pulled forward from W3.6 to W3.4 (2026-06-03)</b>: the DI registrations
/// can't live in <c>LankaConnect.Infrastructure.DependencyInjection</c> after
/// W3.4 because Notifications.Infrastructure references LankaConnect.Infrastructure
/// (for <c>AppDbContext</c> + <c>Repository&lt;T&gt;</c> via the transitional
/// edge) — a reverse reference from Infrastructure would close a cycle. The
/// W3.6 / W3.7 feature-flag wiring and controller move build ON this extension.
/// </para>
/// <para>
/// <b>Registrations</b>:
/// <list type="bullet">
///   <item><see cref="NotificationsDbContext"/> — scoped; same Npgsql wiring as AppDbContext + per-context migrations history table in the <c>notifications</c> schema (set up for the W3.5 baseline migration)</item>
///   <item><see cref="INotificationRepository"/> → <see cref="NotificationRepository"/> — scoped; the implementation still injects <c>AppDbContext</c> through the transitional edge (W3.6 / W3.7 feature flag pivots to <see cref="NotificationsDbContext"/>)</item>
/// </list>
/// </para>
/// <para>
/// MediatR handler scanning for <c>Notifications.Application</c> is performed
/// by the host's <c>builder.Services.AddApplication(...)</c> assembly sweep
/// (the handlers reach the host's MediatR registration because Application
/// → Notifications.Application via the host's ProjectReferences). The module
/// extension intentionally doesn't repeat that sweep to avoid double-registration.
/// </para>
/// </remarks>
public static class NotificationsModule
{
    /// <summary>Per-context migrations history table name (EF convention).</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    /// <summary>
    /// Registers the Notifications module composition: DbContext, repositories,
    /// and (later) the cross-module dispatcher implementation. Safe to call
    /// once per host.
    /// </summary>
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to register NotificationsDbContext.");

        // Wave 8.5.f continuation (2026-07-16, Consult #28 R1): per-module
        // SaveChangesInterceptor dispatches domain events raised on Notification
        // aggregates that would otherwise be dropped when handlers use
        // IMultiContextUnitOfWork.CommitAsync(new DbContext[] { _notificationsContext }, ct)
        // or direct _notificationsDbContext.SaveChangesAsync. Mirrors commit
        // 1212d994 (LankaEvents/Identity/Communications wiring).
        services.AddScoped<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>();

        services.AddDbContext<NotificationsDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, NotificationsDbContext.SchemaName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
            options.AddInterceptors(sp.GetRequiredService<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>());
        }, ServiceLifetime.Scoped);

        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Wave 6.5.c: per-module outbox wiring (producer scoped + OutboxProcessor
        // hosted). The IIntegrationEventOutbox<NotificationsDbContext> adapter is
        // registered by the composition root (LankaConnect.API) via
        // LankaConnect.Infrastructure — it depends on both this project and the
        // BuildingBlocks concrete, so it lives up the graph rather than here.
        services.AddModuleOutbox<NotificationsDbContext>();

        // W3.8 fix Phase A (2026-06-04) — register MediatR handlers from the
        // Notifications.Application assembly. The host's outer `AddApplication(...)`
        // only scans `LankaConnect.Application.dll`; the module's handlers live in
        // a separate assembly and would otherwise be invisible to MediatR.
        // Surfaced as a runtime InvalidOperationException ("No service for type
        // 'MediatR.IRequestHandler<GetUnreadNotificationsQuery, ...>'") on the
        // first /api/Notifications/unread call after the W3.6 controller move.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(LankaConnect.Modules.Notifications.Application.Queries.GetUnreadNotifications.GetUnreadNotificationsQueryHandler)
                .Assembly));

        return services;
    }
}
