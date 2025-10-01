using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Remove default 5-minute clock skew
                RequireExpirationTime = true
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    logger.LogDebug("JWT token validated for user: {UserId}", userId);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT authentication challenge: {Error}", context.Error);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Basic role-based policies
            options.AddPolicy("RequireUser", policy =>
                policy.RequireRole(UserRole.User.ToString(), UserRole.BusinessOwner.ToString(), 
                                 UserRole.Moderator.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy("RequireBusinessOwner", policy =>
                policy.RequireRole(UserRole.BusinessOwner.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy("RequireModerator", policy =>
                policy.RequireRole(UserRole.Moderator.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(UserRole.Admin.ToString()));

            // Email verification requirement
            options.AddPolicy("RequireEmailVerified", policy =>
                policy.RequireClaim("isEmailVerified", "true"));

            // Active account requirement
            options.AddPolicy("RequireActiveAccount", policy =>
                policy.RequireClaim("isActive", "true"));

            // Combined policies
            options.AddPolicy("VerifiedUser", policy =>
            {
                policy.RequireRole(UserRole.User.ToString(), UserRole.BusinessOwner.ToString(), 
                                 UserRole.Moderator.ToString(), UserRole.Admin.ToString());
                policy.RequireClaim("isActive", "true");
                // Note: Email verification check removed for now as it's not in JWT claims yet
            });

            options.AddPolicy("VerifiedBusinessOwner", policy =>
            {
                policy.RequireRole(UserRole.BusinessOwner.ToString(), UserRole.Admin.ToString());
                policy.RequireClaim("isActive", "true");
            });

            options.AddPolicy("ContentManager", policy =>
            {
                policy.RequireRole(UserRole.Moderator.ToString(), UserRole.Admin.ToString());
                policy.RequireClaim("isActive", "true");
            });

            // Custom claim-based policies
            options.AddPolicy("CanManageUsers", policy =>
                policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString()));

            options.AddPolicy("CanManageBusinesses", policy =>
                policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString(), 
                                 UserRole.BusinessOwner.ToString()));

            options.AddPolicy("CanModerateContent", policy =>
                policy.RequireRole(UserRole.Admin.ToString(), UserRole.Moderator.ToString()));
        });

        return services;
    }

    public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        
        return app;
    }
}