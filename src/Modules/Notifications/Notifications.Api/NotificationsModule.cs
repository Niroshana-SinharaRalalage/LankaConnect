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

        services.AddDbContext<NotificationsDbContext>(options =>
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
        }, ServiceLifetime.Scoped);

        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
