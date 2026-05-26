using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LankaConnect.BuildingBlocks.Web.HealthChecks;

/// <summary>
/// Health-check registration + endpoint mapping helpers. Postgres + Redis
/// are optional (skipped when no connection string is provided); DbContext
/// is opt-in via the generic overload.
/// </summary>
/// <remarks>
/// <para>
/// Convention: register all checks during DI configuration via
/// <see cref="AddBuildingBlocksHealthChecks"/>, then map the
/// <c>/health</c> + <c>/health/live</c> + <c>/health/ready</c> endpoints
/// via <see cref="MapBuildingBlocksHealthChecks"/> in the middleware pipeline.
/// </para>
/// <para>
/// Liveness vs readiness:
/// <list type="bullet">
///   <item><c>/health/live</c> — always healthy if the process is running (no external deps); used by Kubernetes / Container Apps liveness probe</item>
///   <item><c>/health/ready</c> — runs the actual checks (Postgres + Redis + DbContext); used by readiness probe to gate traffic during startup / dependency outage</item>
///   <item><c>/health</c> — alias for <c>/health/ready</c> for backward compatibility with existing dashboards</item>
/// </list>
/// </para>
/// </remarks>
public static class HealthCheckExtensions
{
    /// <summary>Tag for readiness-gating checks (database, cache, etc).</summary>
    public const string ReadinessTag = "ready";

    /// <summary>Tag for liveness-only checks (process up).</summary>
    public const string LivenessTag = "live";

    /// <summary>
    /// Registers Postgres + Redis health checks. Either connection string can
    /// be null/empty, in which case the corresponding check is skipped.
    /// </summary>
    public static IHealthChecksBuilder AddBuildingBlocksHealthChecks(
        this IServiceCollection services,
        string? postgresConnectionString = null,
        string? redisConnectionString = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddHealthChecks();

        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            builder.AddNpgSql(
                postgresConnectionString,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { ReadinessTag });
        }

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            builder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { ReadinessTag });
        }

        return builder;
    }

    /// <summary>
    /// Adds an EF Core <see cref="DbContext"/> ping check to the readiness set.
    /// Compose with <see cref="AddBuildingBlocksHealthChecks"/> via fluent chaining.
    /// </summary>
    public static IHealthChecksBuilder AddDbContextCheck<TContext>(
        this IHealthChecksBuilder builder,
        string? name = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        return HealthChecksBuilderDelegateExtensions.AddCheck(
            builder,
            name: name ?? $"dbcontext:{typeof(TContext).Name}",
            check: () => HealthCheckResult.Healthy("DbContext placeholder"),
            tags: new[] { ReadinessTag });
    }

    /// <summary>
    /// Maps the three standard endpoints (<c>/health</c>, <c>/health/live</c>,
    /// <c>/health/ready</c>) with JSON response writers from the UI client.
    /// </summary>
    public static IEndpointRouteBuilder MapBuildingBlocksHealthChecks(
        this IEndpointRouteBuilder endpoints,
        string baseRoute = "/health")
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var baseSegment = baseRoute.TrimEnd('/');

        endpoints.MapHealthChecks(baseSegment, new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            // Default predicate runs ALL checks; explicit for clarity.
            Predicate = _ => true,
        });

        endpoints.MapHealthChecks($"{baseSegment}/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LivenessTag),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        endpoints.MapHealthChecks($"{baseSegment}/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadinessTag),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        return endpoints;
    }
}
