using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Security.Services;

/// <summary>
/// Service for validating Microsoft Entra External ID tokens and retrieving user information
/// </summary>
public class EntraExternalIdService : IEntraExternalIdService
{
    private readonly EntraExternalIdOptions _options;
    private readonly ILogger<EntraExternalIdService> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public EntraExternalIdService(
        IOptions<EntraExternalIdOptions> options,
        ILogger<EntraExternalIdService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();

        // Configure OpenID Connect metadata endpoint for Entra External ID
        var metadataAddress = $"{_options.Instance}{_options.TenantId}/v2.0/.well-known/openid-configuration";

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        _logger.LogInformation("EntraExternalIdService initialized for tenant: {TenantId}", _options.TenantId);
    }

    /// <inheritdoc/>
    public bool IsEnabled => _options.IsEnabled;

    /// <inheritdoc/>
    public async Task<Result<EntraTokenClaims>> ValidateAccessTokenAsync(string accessToken)
    {
        if (!_options.IsEnabled)
        {
            return Result<EntraTokenClaims>.Failure("Entra External ID integration is not enabled");
        }

        try
        {
            // Get the OpenID Connect configuration from Entra
            var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);

            // Configure token validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _options.TokenValidation.ValidateIssuer,
                ValidIssuer = $"{_options.Instance}{_options.TenantId}/v2.0",
                ValidateAudience = _options.TokenValidation.ValidateAudience,
                ValidAudience = _options.ClientId,
                ValidateIssuerSigningKey = _options.TokenValidation.ValidateIssuerSigningKey,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = _options.TokenValidation.ValidateLifetime,
                ClockSkew = TimeSpan.FromSeconds(_options.TokenValidation.ClockSkewSeconds)
            };

            // Validate the token
            var principal = _tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("Token validation failed: Not a valid JWT token");
                return Result<EntraTokenClaims>.Failure("Invalid token format");
            }

            // Extract claims
            var claims = new EntraTokenClaims
            {
                ObjectId = GetClaimValue(principal, "oid") ?? GetClaimValue(principal, "sub") ?? string.Empty,
                Email = GetClaimValue(principal, "email") ?? GetClaimValue(principal, "preferred_username") ?? string.Empty,
                Name = GetClaimValue(principal, "name"),
                GivenName = GetClaimValue(principal, "given_name"),
                FamilyName = GetClaimValue(principal, "family_name"),
                AllClaims = principal.Claims.ToDictionary(c => c.Type, c => c.Value)
            };

            // Validate required claims
            if (string.IsNullOrEmpty(claims.ObjectId))
            {
                _logger.LogWarning("Token validation failed: Missing OID claim");
                return Result<EntraTokenClaims>.Failure("Token missing required object ID (OID) claim");
            }

            if (string.IsNullOrEmpty(claims.Email))
            {
                _logger.LogWarning("Token validation failed: Missing email claim");
                return Result<EntraTokenClaims>.Failure("Token missing required email claim");
            }

            _logger.LogInformation("Successfully validated Entra token for user: {Email}", claims.Email);
            return Result<EntraTokenClaims>.Success(claims);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Token expired");
            return Result<EntraTokenClaims>.Failure("Access token has expired");
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid issuer");
            return Result<EntraTokenClaims>.Failure("Invalid token issuer");
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid audience");
            return Result<EntraTokenClaims>.Failure("Invalid token audience");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid token");
            return Result<EntraTokenClaims>.Failure("Invalid access token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating Entra token");
            return Result<EntraTokenClaims>.Failure("Token validation failed due to an unexpected error");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<EntraUserInfo>> GetUserInfoAsync(string accessToken)
    {
        if (!_options.IsEnabled)
        {
            return Result<EntraUserInfo>.Failure("Entra External ID integration is not enabled");
        }

        // First validate the token and extract claims
        var tokenValidationResult = await ValidateAccessTokenAsync(accessToken);
        if (tokenValidationResult.IsFailure)
        {
            return Result<EntraUserInfo>.Failure(tokenValidationResult.Errors);
        }

        var claims = tokenValidationResult.Value;

        try
        {
            // Map token claims to user info
            var userInfo = new EntraUserInfo
            {
                ObjectId = claims.ObjectId,
                Email = claims.Email,
                FirstName = claims.GivenName ?? ExtractFirstName(claims.Name),
                LastName = claims.FamilyName ?? ExtractLastName(claims.Name),
                DisplayName = claims.Name,
                EmailVerified = true // Entra pre-verifies emails
            };

            // Validate required fields
            if (string.IsNullOrEmpty(userInfo.FirstName))
            {
                _logger.LogWarning("User info missing first name for {Email}", userInfo.Email);
                userInfo.FirstName = "User"; // Default fallback
            }

            if (string.IsNullOrEmpty(userInfo.LastName))
            {
                _logger.LogWarning("User info missing last name for {Email}", userInfo.Email);
                userInfo.LastName = "Unknown"; // Default fallback
            }

            _logger.LogInformation("Successfully retrieved user info for: {Email}", userInfo.Email);
            return Result<EntraUserInfo>.Success(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info from Entra token");
            return Result<EntraUserInfo>.Failure("Failed to retrieve user information");
        }
    }

    /// <summary>
    /// Extracts a specific claim value from the claims principal
    /// </summary>
    private string? GetClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// Extracts first name from full name if given_name claim is not available
    /// </summary>
    private string ExtractFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "User";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "User";
    }

    /// <summary>
    /// Extracts last name from full name if family_name claim is not available
    /// </summary>
    private string ExtractLastName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "Unknown";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[^1] : "Unknown";
    }
}
