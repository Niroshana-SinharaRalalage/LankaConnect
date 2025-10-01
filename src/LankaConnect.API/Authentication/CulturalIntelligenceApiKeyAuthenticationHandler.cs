using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LankaConnect.API.Authentication;

/// <summary>
/// API Key authentication handler for Cultural Intelligence APIs
/// Implements tiered access control: Free, Premium, Enterprise
/// </summary>
public class CulturalIntelligenceApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ILogger<CulturalIntelligenceApiKeyAuthenticationHandler> _logger;

    public CulturalIntelligenceApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _logger = logger.CreateLogger<CulturalIntelligenceApiKeyAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Check for API key in header
            if (!Request.Headers.ContainsKey("X-API-Key"))
            {
                _logger.LogWarning("Missing X-API-Key header for Cultural Intelligence API");
                return AuthenticateResult.Fail("Missing API Key");
            }

            var apiKey = Request.Headers["X-API-Key"].ToString();
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Empty API key provided");
                return AuthenticateResult.Fail("Empty API Key");
            }

            // Validate API key and determine tier
            var apiKeyValidation = await ValidateApiKey(apiKey);
            
            if (!apiKeyValidation.IsValid)
            {
                _logger.LogWarning("Invalid API key provided: {ApiKeyPrefix}", apiKey.Substring(0, Math.Min(8, apiKey.Length)));
                return AuthenticateResult.Fail("Invalid API Key");
            }

            // Create claims based on API key tier
            var claims = new List<Claim>
            {
                new Claim("ApiKey", apiKey),
                new Claim("ApiKeyTier", apiKeyValidation.Tier),
                new Claim("RateLimit", apiKeyValidation.RateLimit.ToString()),
                new Claim("UsageAnalytics", apiKeyValidation.AllowsUsageAnalytics.ToString()),
                new Claim(ClaimTypes.Name, $"CulturalIntelligenceApi-{apiKeyValidation.Tier}"),
                new Claim(ClaimTypes.NameIdentifier, apiKeyValidation.ClientId)
            };

            // Add premium features claims
            if (apiKeyValidation.Tier == "Premium" || apiKeyValidation.Tier == "Enterprise")
            {
                claims.Add(new Claim("PremiumFeatures", "true"));
                claims.Add(new Claim("AdvancedAnalytics", "true"));
            }

            if (apiKeyValidation.Tier == "Enterprise")
            {
                claims.Add(new Claim("WhiteLabel", "true"));
                claims.Add(new Claim("CustomIntegrations", "true"));
            }

            var identity = new ClaimsIdentity(claims, "ApiKey");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "ApiKey");

            _logger.LogInformation("Successfully authenticated API key with tier {Tier}", apiKeyValidation.Tier);
            
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API key authentication");
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    private async Task<ApiKeyValidationResult> ValidateApiKey(string apiKey)
    {
        // In production, this would validate against a database or external service
        // For now, implementing a basic validation with test keys
        
        await Task.Delay(1); // Simulate async validation
        
        // Test API keys for different tiers
        var validApiKeys = new Dictionary<string, ApiKeyValidationResult>
        {
            // Free tier - basic cultural intelligence
            ["ci-free-test-key-123"] = new ApiKeyValidationResult
            {
                IsValid = true,
                Tier = "Free",
                RateLimit = 100, // 100 requests per minute
                AllowsUsageAnalytics = false,
                ClientId = "free-client-001"
            },
            
            // Premium tier - advanced features
            ["ci-premium-test-key-456"] = new ApiKeyValidationResult
            {
                IsValid = true,
                Tier = "Premium",
                RateLimit = 1000, // 1000 requests per minute
                AllowsUsageAnalytics = true,
                ClientId = "premium-client-001"
            },
            
            // Enterprise tier - full access
            ["ci-enterprise-test-key-789"] = new ApiKeyValidationResult
            {
                IsValid = true,
                Tier = "Enterprise",
                RateLimit = 5000, // 5000 requests per minute
                AllowsUsageAnalytics = true,
                ClientId = "enterprise-client-001"
            }
        };

        if (validApiKeys.TryGetValue(apiKey, out var result))
        {
            return result;
        }

        // Check for pattern-based validation (in real implementation, would be database lookup)
        if (apiKey.StartsWith("ci-") && apiKey.Length >= 20)
        {
            // Extract tier from key pattern
            if (apiKey.Contains("-free-"))
            {
                return new ApiKeyValidationResult
                {
                    IsValid = true,
                    Tier = "Free",
                    RateLimit = 100,
                    AllowsUsageAnalytics = false,
                    ClientId = $"client-{apiKey.GetHashCode():X8}"
                };
            }
            
            if (apiKey.Contains("-premium-"))
            {
                return new ApiKeyValidationResult
                {
                    IsValid = true,
                    Tier = "Premium",
                    RateLimit = 1000,
                    AllowsUsageAnalytics = true,
                    ClientId = $"client-{apiKey.GetHashCode():X8}"
                };
            }
            
            if (apiKey.Contains("-enterprise-"))
            {
                return new ApiKeyValidationResult
                {
                    IsValid = true,
                    Tier = "Enterprise",
                    RateLimit = 5000,
                    AllowsUsageAnalytics = true,
                    ClientId = $"client-{apiKey.GetHashCode():X8}"
                };
            }
        }

        return new ApiKeyValidationResult { IsValid = false };
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!Response.HasStarted)
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                Error = "Unauthorized",
                Message = "Valid API key required. Include X-API-Key header with your request.",
                Documentation = "https://docs.lankaconnect.com/cultural-intelligence-api/authentication"
            };
            
            await Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        if (!Response.HasStarted)
        {
            Response.StatusCode = 403;
            Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                Error = "Forbidden", 
                Message = "Your API key does not have sufficient permissions for this resource.",
                UpgradeInfo = "https://lankaconnect.com/cultural-intelligence-api/pricing"
            };
            
            await Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}

/// <summary>
/// Result of API key validation with tier information
/// </summary>
public class ApiKeyValidationResult
{
    public bool IsValid { get; set; }
    public string Tier { get; set; } = string.Empty;
    public int RateLimit { get; set; }
    public bool AllowsUsageAnalytics { get; set; }
    public string ClientId { get; set; } = string.Empty;
}

/// <summary>
/// Helper methods for API key management
/// </summary>
public static class CulturalIntelligenceApiHelpers
{
    /// <summary>
    /// Extract API key from HTTP context for rate limiting partitioning
    /// </summary>
    public static string? GetApiKey(HttpContext context)
    {
        return context.Request.Headers["X-API-Key"].FirstOrDefault();
    }

    /// <summary>
    /// Get API key tier from claims
    /// </summary>
    public static string GetApiKeyTier(ClaimsPrincipal user)
    {
        return user.FindFirst("ApiKeyTier")?.Value ?? "Free";
    }

    /// <summary>
    /// Check if user has premium features access
    /// </summary>
    public static bool HasPremiumFeatures(ClaimsPrincipal user)
    {
        return user.HasClaim("PremiumFeatures", "true");
    }

    /// <summary>
    /// Check if user has enterprise features access
    /// </summary>
    public static bool HasEnterpriseFeatures(ClaimsPrincipal user)
    {
        return user.HasClaim("WhiteLabel", "true");
    }
}