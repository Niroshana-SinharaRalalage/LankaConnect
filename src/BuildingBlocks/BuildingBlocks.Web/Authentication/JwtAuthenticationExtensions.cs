using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LankaConnect.BuildingBlocks.Web.Authentication;

/// <summary>
/// Cross-cutting JWT bearer authentication wiring. Mirrors the pattern in the
/// existing <c>LankaConnect.Hosts.AllInOne.Extensions.AuthenticationExtensions</c> but
/// (a) lives in BuildingBlocks so future modules can consume it, (b) accepts
/// a strongly-typed <see cref="JwtSettings"/> section, (c) defaults safer
/// (HTTPS required, zero clock skew) with explicit opt-out.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication using settings from the configuration
    /// section named by <paramref name="sectionName"/> (default <c>"Jwt"</c>).
    /// Validates issuer, audience, signing key, lifetime, and requires an
    /// expiration claim.
    /// </summary>
    /// <exception cref="InvalidOperationException">If Key/Issuer/Audience are missing.</exception>
    public static IServiceCollection AddBuildingBlocksJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = JwtSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = new JwtSettings();
        configuration.GetSection(sectionName).Bind(settings);

        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new InvalidOperationException(
                $"JWT Key is not configured. Set '{sectionName}:Key' in configuration.");
        }
        if (string.IsNullOrWhiteSpace(settings.Issuer))
        {
            throw new InvalidOperationException(
                $"JWT Issuer is not configured. Set '{sectionName}:Issuer' in configuration.");
        }
        if (string.IsNullOrWhiteSpace(settings.Audience))
        {
            throw new InvalidOperationException(
                $"JWT Audience is not configured. Set '{sectionName}:Audience' in configuration.");
        }

        services.AddSingleton(settings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateLifetime = true,
                ClockSkew = settings.ClockSkew,
                RequireExpirationTime = true,
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("BuildingBlocks.Web.Jwt");
                    logger.LogWarning(ctx.Exception, "JWT authentication FAILED");
                    return Task.CompletedTask;
                },
                OnTokenValidated = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("BuildingBlocks.Web.Jwt");
                    var userId = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    logger.LogDebug("JWT token validated for {UserId}", userId ?? "(anonymous)");
                    return Task.CompletedTask;
                },
                OnChallenge = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("BuildingBlocks.Web.Jwt");
                    logger.LogWarning("JWT challenge: {Error} (description: {Description})",
                        ctx.Error, ctx.ErrorDescription);
                    return Task.CompletedTask;
                },
            };
        });

        services.AddAuthorization();

        return services;
    }
}
