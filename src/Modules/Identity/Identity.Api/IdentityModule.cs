using FluentValidation;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Identity.Application.Commands;
using LankaConnect.Modules.Identity.Application.Queries;
using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.Modules.Identity.Infrastructure.Data;
using LankaConnect.Modules.Identity.Infrastructure.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace LankaConnect.Modules.Identity.Api;

/// <summary>
/// Composition-root DI extension for the Identity module. Mirrors the
/// W4.4 Payments / W5.4 Communications / W5.3 Forms composition pattern.
/// </summary>
/// <remarks>
/// Wave 4.6.b (2026-06-24). Today this module registers only the cross-module
/// Contracts surface (<see cref="IIdentityQueries"/>) + scans Identity.Application
/// for MediatR / FluentValidation registrations. The underlying
/// <c>IUserRepository</c> is still wired by <c>AddInfrastructure</c> until
/// Wave 4.6.d.2 physical move. <see cref="IIdentityCommands"/> adapter +
/// the moved Auth/Users command + query handlers land at 4.6.c.1 - c.4.
/// <c>CurrentUserService</c> adapter relocates here at 4.6.c.5 + this method
/// gains the matching <c>AddScoped&lt;ICurrentUserService, CurrentUserService&gt;()</c>
/// call.
/// </remarks>
public static class IdentityModule
{
    /// <summary>Per-context migrations history table name (EF convention).</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 4C.e (2026-07-08): IdentityDbContext registration per Consult #14 PASS B.
        // Dormant until 4C.e.2 caller cutover — dual-mapped with AppDbContext until
        // the ~18 direct-Users callers switch DI targets. Mirrors the Media/Forms/
        // LankaEvents module pattern.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to register IdentityDbContext.");

        // Wave 8.5.f (2026-07-15, Consult #25 Q2 prereq): per-module SaveChangesInterceptor
        // dispatches domain events raised on User/UserWhatsAppPreferences aggregates that
        // would otherwise be dropped when handlers use direct `_identityDbContext.SaveChangesAsync`
        // (e.g. RegisterUserHandler + UpdateUserPreferredMetroAreasCommandHandler post-sprint).
        services.AddScoped<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>();

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(IdentityDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, IdentityDbContext.SchemaName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
            options.AddInterceptors(sp.GetRequiredService<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>());
        }, ServiceLifetime.Scoped);

        // 4C.e: per-module outbox wire-up (mirrors MediaModule 6.5.b canary).
        services.AddModuleOutbox<IdentityDbContext>();

        // Wave 4.6.b cross-module Contracts surface.
        services.AddScoped<IIdentityQueries, IdentityQueries>();

        // Wave 4.7.e (2026-06-26): UserRepository registration moved from
        // LankaConnect.Infrastructure.DependencyInjection alongside the
        // physical User aggregate move to Identity.Domain / Identity.Infrastructure.
        services.AddScoped<LankaConnect.Modules.Identity.Domain.Repositories.IUserRepository,
            LankaConnect.Modules.Identity.Infrastructure.Repositories.UserRepository>();
        // Wave 4.6.d.1 (2026-06-24): IIdentityCommands semantic mutators per
        // architect Risk #3 Option A. Owns hashing/throttle/lifetime/refresh-token
        // revocation. Communications.SendPasswordReset + ResetPassword handlers
        // swap from IUserRepository + IPasswordHashingService to this surface.
        services.AddScoped<IIdentityCommands, IdentityCommands>();

        // MediatR / FluentValidation scan of Identity.Application -- pulled
        // forward of the 4.6.c.1 handler moves so subsequent moves don't have
        // to also re-wire DI. Mirrors the PaymentsModule 4.4.b pattern. The
        // Identity.Application assembly is currently empty of handlers; the
        // scan is a no-op until 4.6.c.1 lands the first command handler.
        var identityAppAssembly = typeof(IdentityQueries).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(identityAppAssembly));
        services.AddValidatorsFromAssembly(identityAppAssembly);

        // Wave 4.6.c.5 (2026-06-24): 4 security adapter registrations relocated
        // from LankaConnect.Infrastructure.DependencyInjection alongside the
        // physical file move into Identity.Infrastructure.Security. Mirrors
        // the W4.4.d.2 PaymentsModule repository registration pattern.
        // Ports stay in LankaConnect.BuildingBlocks.Application.Common.Interfaces per
        // architect Risk #2 Option C (signatures take User type so the ports
        // cannot promote to Contracts purity). ICurrentUserService is the
        // exception -- it moved to Identity.Contracts at 4.6.a.
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEntraExternalIdService, EntraExternalIdService>();

        return services;
    }
}
