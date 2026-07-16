using FluentValidation;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Forms.Application.Commands;
using LankaConnect.Modules.Forms.Application.Queries;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using LankaConnect.Modules.Forms.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.Forms.Api;

/// <summary>
/// Composition-root DI extension for the Forms module. Mirrors the W4.2 Media
/// composition pattern.
/// </summary>
public static class FormsModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public static IServiceCollection AddFormsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to register FormsDbContext.");

        // Hotfix 2026-06-12 (surfaced by Wave 5.3d.2 Smoke-CancelRsvp).
        // FormResponse.SelectedOptionIds is a List<Guid> jsonb column — writing it
        // via Npgsql 8+ requires EnableDynamicJson() on the data source. Mirrors
        // src/LankaConnect.Infrastructure/DependencyInjection.cs:73 for AppDbContext.
        // Without this opt-in, every POST /api/Events/{eventId}/forms/{formId}/responses
        // throws InvalidCastException at save (broken since W4.3 2026-06-06).
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        // Wave 8.5.f continuation (2026-07-16, Consult #28 R1): per-module
        // SaveChangesInterceptor dispatches domain events raised on Form /
        // FormResponse aggregates that would otherwise be dropped when handlers
        // use IMultiContextUnitOfWork.CommitAsync(new DbContext[] { _formsContext }, ct)
        // or direct _formsDbContext.SaveChangesAsync. Mirrors commit 1212d994
        // (LankaEvents/Identity/Communications wiring).
        services.AddScoped<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>();

        services.AddDbContext<FormsDbContext>((sp, options) =>
        {
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(FormsDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, FormsDbContext.SchemaName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
            options.AddInterceptors(sp.GetRequiredService<LankaConnect.BuildingBlocks.Infrastructure.Persistence.DomainEventSaveChangesInterceptor>());
        }, ServiceLifetime.Scoped);

        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFormResponseRepository, FormResponseRepository>();

        // Wave 6.5.d: per-module outbox wiring (producer scoped + OutboxProcessor
        // hosted). The IIntegrationEventOutbox<FormsDbContext> adapter is
        // registered by the composition root (LankaConnect.API) via
        // LankaConnect.Infrastructure — it depends on both this project and the
        // BuildingBlocks concrete, so it lives up the graph rather than here.
        services.AddModuleOutbox<FormsDbContext>();

        // Wave 5.3b (2026-06-11): cross-module Contracts surface — IFormQueries +
        // IFormCommands. Cross-module consumers (Wave 5.3d EventHandlers in
        // LankaConnect.Application.Events.*) inject these interfaces instead of
        // IFormRepository, preserving the ArchTest module boundary.
        services.AddScoped<IFormQueries, FormQueries>();
        services.AddScoped<IFormCommands, FormCommands>();

        // Wave 5.3c (2026-06-12): Forms.Application now hosts the 13 command
        // handlers (5.3c.1) and 7 query handlers (5.3c.2) that moved out of
        // LankaConnect.Application. LankaConnect.Application's DependencyInjection
        // only scans its own assembly for MediatR / FluentValidation, so the
        // moved handlers are invisible to the global container without explicit
        // registration here. Scan the Forms.Application assembly for both
        // MediatR handlers and FluentValidation validators (CreateEventFormCommandValidator
        // et al. moved as part of 5.3c.1).
        var formsAppAssembly = typeof(FormQueries).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(formsAppAssembly));
        services.AddValidatorsFromAssembly(formsAppAssembly);

        return services;
    }
}
