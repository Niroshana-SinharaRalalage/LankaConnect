using FluentValidation;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Communications.Domain.Repositories;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Application.Queries;
using LankaConnect.Modules.Communications.Contracts;
using LankaConnect.Modules.Communications.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace LankaConnect.Modules.Communications.Api;

/// <summary>
/// Composition-root DI extension for the Communications module. Mirrors the
/// W4.2 Media / W5.3b Forms composition pattern.
/// </summary>
/// <remarks>
/// Wave 5.4.b (2026-06-13). Today this module only registers the cross-module
/// Contracts surface (<see cref="IEmailGroupQueries"/>) so consumers can take
/// the dependency without reaching into <c>LankaConnect.Domain.Communications</c>.
/// <para>
/// The underlying <c>IEmailGroupRepository</c> is still wired in
/// <c>LankaConnect.Infrastructure.DependencyInjection</c> (the EmailGroup
/// aggregate moves to Communications.Domain in Wave 5.4.d.2). When the move
/// lands, the repository registration relocates here, the DbContext (or
/// AppDbContext-share) joins, and a MediatR / FluentValidation assembly scan
/// for the moved Communications.Application handlers is added.
/// </para>
/// </remarks>
public static class CommunicationsModule
{
    /// <summary>Per-context migrations history table name (EF convention).</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public static IServiceCollection AddCommunicationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Day 6 hotfix (2026-07-10, post-Consult #18 deploy attempt): CommunicationsDbContext
        // DI registration was missing from all module DI extensions, causing NewsletterEmailJob +
        // 5 Newsletter command/query handlers to fail DI validation on host startup. Mirrors the
        // IdentityModule 4C.e / MediaModule 6.5.b canary / LankaEventsModule 6.5.f patterns.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to register CommunicationsDbContext.");

        services.AddDbContext<CommunicationsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(CommunicationsDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, CommunicationsDbContext.SchemaName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
        }, ServiceLifetime.Scoped);

        // Per-module outbox (mirrors IdentityModule 4C.e + MediaModule 6.5.b canary).
        services.AddModuleOutbox<CommunicationsDbContext>();

        // Wave 5.4.b cross-module Contracts surface.
        services.AddScoped<IEmailGroupQueries, EmailGroupQueries>();

        // Wave 5.4.d.2 (2026-06-22): IEmailGroupRepository registration moved
        // here from LankaConnect.Infrastructure.DependencyInjection alongside
        // the physical EmailGroupRepository file move. The repository still
        // shares AppDbContext (no separate CommunicationsDbContext in W5.4);
        // ctor injects via the W5.4.d.2 transitional LankaConnect.Infrastructure
        // ProjectReference.
        services.AddScoped<IEmailGroupRepository, LankaConnect.Modules.Communications.Infrastructure.Repositories.EmailGroupRepository>();

        // Wave 5.4.c.1 (2026-06-22): Communications.Application now hosts the 5
        // command handlers (CreateEmailGroup / UpdateEmailGroup / DeleteEmailGroup
        // / CreateNewsletter / UpdateNewsletter) that moved out of
        // LankaConnect.Application. The legacy DependencyInjection.cs only scans
        // its own assembly for MediatR / FluentValidation, so the moved handlers
        // + validators are invisible to the global container without explicit
        // registration here. Scan the Communications.Application assembly for
        // both handler kinds (mirrors the FormsModule 5.3c.2 hotfix pattern).
        var commsAppAssembly = typeof(EmailGroupQueries).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(commsAppAssembly));
        services.AddValidatorsFromAssembly(commsAppAssembly);

        return services;
    }
}
