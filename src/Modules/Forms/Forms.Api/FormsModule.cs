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

        services.AddDbContext<FormsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(FormsDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, FormsDbContext.SchemaName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
        }, ServiceLifetime.Scoped);

        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFormResponseRepository, FormResponseRepository>();

        // Wave 5.3b (2026-06-11): cross-module Contracts surface — IFormQueries +
        // IFormCommands. Cross-module consumers (Wave 5.3d EventHandlers in
        // LankaConnect.Application.Events.*) inject these interfaces instead of
        // IFormRepository, preserving the ArchTest module boundary.
        services.AddScoped<IFormQueries, FormQueries>();
        services.AddScoped<IFormCommands, FormCommands>();

        return services;
    }
}
