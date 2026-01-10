using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;

namespace LankaConnect.Infrastructure.Security.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly IDistributedCache _cache;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtTokenService(
        IConfiguration configuration, 
        ILogger<JwtTokenService> logger,
        IDistributedCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew
        };
    }

    public Task<Result<string>> GenerateAccessTokenAsync(User user)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? 
                throw new InvalidOperationException("JWT Key not configured"));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email.Value),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role.ToString()), // BUGFIX: Add role claim for authorization policies
                new("firstName", user.FirstName),
                new("lastName", user.LastName),
                new("isActive", user.IsActive.ToString().ToLower()),
                new("isEmailVerified", user.IsEmailVerified.ToString().ToLower()), // FIX: Add email verification status to JWT
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            // Add phone number if available
            if (user.PhoneNumber != null)
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber.Value));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(
                    _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15)),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("Access token generated for user {UserId}", user.Id);
            return Task.FromResult(Result<string>.Success(tokenString));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access token for user {UserId}", user.Id);
            return Task.FromResult(Result<string>.Failure("Failed to generate access token"));
        }
    }

    public Task<Result<string>> GenerateRefreshTokenAsync()
    {
        try
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var refreshToken = Convert.ToBase64String(randomBytes);

            _logger.LogDebug("Refresh token generated");
            return Task.FromResult(Result<string>.Success(refreshToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token");
            return Task.FromResult(Result<string>.Failure("Failed to generate refresh token"));
        }
    }

    public async Task<Result<Guid>> ValidateTokenAsync(string token)
    {
        try
        {
            // Check if token is blacklisted
            var blacklistKey = $"blacklisted_token:{token}";
            var isBlacklisted = await _cache.GetStringAsync(blacklistKey);
            if (!string.IsNullOrEmpty(isBlacklisted))
            {
                _logger.LogWarning("Attempt to use blacklisted token");
                return Result<Guid>.Failure("Token has been invalidated");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Result<Guid>.Failure("Invalid token algorithm");
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Result<Guid>.Failure("Invalid user ID in token");
            }

            _logger.LogDebug("Token validated for user {UserId}", userId);
            return Result<Guid>.Success(userId);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token validation failed: Token expired");
            return Result<Guid>.Failure("Token has expired");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid token");
            return Result<Guid>.Failure("Invalid token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Result<Guid>.Failure("Token validation failed");
        }
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        var result = await ValidateTokenAsync(token);
        return result.IsSuccess;
    }

    public async Task<Result> InvalidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var blacklistKey = $"blacklisted_refresh_token:{refreshToken}";
            var expirationTime = TimeSpan.FromDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
            
            await _cache.SetStringAsync(blacklistKey, "true", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            });

            _logger.LogInformation("Refresh token invalidated");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating refresh token");
            return Result.Failure("Failed to invalidate refresh token");
        }
    }

    public async Task<Result> InvalidateAllUserTokensAsync(Guid userId)
    {
        try
        {
            // Store a timestamp indicating when all tokens for this user were invalidated
            var invalidationKey = $"user_tokens_invalidated:{userId}";
            var invalidationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            var expirationTime = TimeSpan.FromDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
            
            await _cache.SetStringAsync(invalidationKey, invalidationTime, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            });

            _logger.LogInformation("All tokens invalidated for user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all user tokens for user {UserId}", userId);
            return Result.Failure("Failed to invalidate user tokens");
        }
    }
}