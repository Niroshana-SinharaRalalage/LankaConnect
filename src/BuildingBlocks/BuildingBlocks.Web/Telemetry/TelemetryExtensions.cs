using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LankaConnect.BuildingBlocks.Web.Telemetry;

/// <summary>
/// OpenTelemetry + Azure Monitor (Application Insights) wiring per ADR-004
/// (architect amendment moved observability from Week 10 → Week 2 so traces
/// land before any module extraction lights up).
/// </summary>
/// <remarks>
/// <para>
/// When an Application Insights connection string is present in configuration
/// (key <c>ApplicationInsights:ConnectionString</c> or env var
/// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c>), the Azure Monitor distro
/// is wired — traces / metrics / logs export to App Insights via OTLP.
/// </para>
/// <para>
/// When NO connection string is configured (local dev), the OTel pipeline
/// still initializes with ASP.NET Core + HttpClient instrumentation but with
/// no exporter — the cost is negligible and the activity sources are still
/// emitted so a local dev can attach a different exporter via configuration.
/// </para>
/// </remarks>
public static class TelemetryExtensions
{
    /// <summary>Configuration key holding the App Insights connection string.</summary>
    public const string ConfigKey = "ApplicationInsights:ConnectionString";

    /// <summary>Environment-variable convention recognized by the Azure Monitor SDK.</summary>
    public const string ConnectionStringEnvVar = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Registers OpenTelemetry with the Azure Monitor exporter when a
    /// connection string is configured. Always registers the OTel host
    /// (<see cref="OpenTelemetryServicesExtensions.AddOpenTelemetry"/>)
    /// so downstream code can add custom activity sources.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <param name="serviceName">
    /// Resource attribute <c>service.name</c> attached to every span / metric.
    /// Use the host's assembly name (e.g. <c>"LankaConnect.Hosts.AllInOne"</c>) for
    /// correlation in App Insights.
    /// </param>
    public static IServiceCollection AddBuildingBlocksTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var connectionString = ResolveConnectionString(configuration);

        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName));

        // W2.6b polish (2026-06-04). Azure Monitor distro auto-instruments
        // AspNetCore + HttpClient + Microsoft.Data.SqlClient — but NOT Npgsql.
        // Npgsql 6+ ships its own ActivitySource named "Npgsql"; adding it
        // here surfaces every Postgres query as a `dependency` span in App
        // Insights with command text, target server, duration, and result
        // status — regardless of which exporter branch runs below.
        //
        // No PackageReference needed — the Npgsql ActivitySource is emitted
        // by the same Npgsql.dll already pulled in via
        // Npgsql.EntityFrameworkCore.PostgreSQL transitively.
        otelBuilder.WithTracing(tracing => tracing.AddSource("Npgsql"));

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            otelBuilder.UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            });
        }
        else
        {
            // No App Insights connection string — still register AspNetCore +
            // HttpClient instrumentation so activity sources are emitted.
            // Local dev can attach a different exporter (Console / Jaeger /
            // Zipkin) via configuration later.
            otelBuilder.WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());
        }

        return services;
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        // Configuration section takes precedence — appsettings.{env}.json + Key Vault.
        var fromConfig = configuration[ConfigKey];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig;
        }

        // Fall back to the env-var convention so a Container App's APP_INSIGHTS
        // connection string set as a secret env var Just Works.
        return Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
    }
}
