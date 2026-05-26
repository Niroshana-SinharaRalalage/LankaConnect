using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.BuildingBlocks.Web.RateLimiting;

/// <summary>
/// Rate limiter registration helpers. Callers compose named policies on top of
/// the defaults; existing app-specific policies (e.g. <c>sponsor-staging-upload</c>
/// in <c>LankaConnect.API/Program.cs</c>) remain owned by the host.
/// </summary>
/// <remarks>
/// Default behavior:
/// <list type="bullet">
///   <item>Rejection status code = 429 Too Many Requests</item>
///   <item>One built-in policy <see cref="PerIpFixedWindowPolicy"/> — 60 requests / minute per remote IP</item>
/// </list>
/// </remarks>
public static class RateLimitingExtensions
{
    /// <summary>Policy name applied per remote IP at a 60-req/min fixed window.</summary>
    public const string PerIpFixedWindowPolicy = "perip-fixedwindow";

    /// <summary>
    /// Registers the rate limiter with the building-block default policy.
    /// Pass <paramref name="configure"/> to add app-specific policies.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksRateLimiter(
        this IServiceCollection services,
        Action<RateLimiterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(PerIpFixedWindowPolicy, httpContext =>
            {
                var ip = ResolveClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ip,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });

            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// Resolves the client IP from <c>Connection.RemoteIpAddress</c> with an
    /// <c>X-Forwarded-For</c> fallback (Container Apps fronts traffic with a
    /// reverse proxy, so the connection IP is the proxy unless forwarded
    /// headers are honored).
    /// </summary>
    private static string ResolveClientIp(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(ip))
        {
            return ip;
        }

        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return "anonymous";
    }
}
