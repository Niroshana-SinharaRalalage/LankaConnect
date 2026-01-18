using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Auth.Commands.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;
    private readonly ITokenConfiguration _tokenConfiguration;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        LankaConnect.Domain.Common.IUnitOfWork unitOfWork,
        ITokenConfiguration tokenConfiguration,
        ILogger<RefreshTokenHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _tokenConfiguration = tokenConfiguration;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RefreshToken"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("IpAddress", request.IpAddress ?? "unknown"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("RefreshToken START: IpAddress={IpAddress}", request.IpAddress ?? "unknown");

            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RefreshToken VALIDATION FAILED: Refresh token missing - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result<RefreshTokenResponse>.Failure("Refresh token is required");
                }

                // Find user by refresh token
                var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RefreshToken FAILED: Invalid refresh token - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result<RefreshTokenResponse>.Failure("Invalid refresh token");
                }

                // Add UserId to LogContext now that we have it
                using (LogContext.PushProperty("UserId", user.Id))
                {
                    // Get the refresh token
                    var refreshToken = user.GetRefreshToken(request.RefreshToken);
                    if (refreshToken == null || !refreshToken.IsActive)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "RefreshToken FAILED: Token invalid or expired - UserId={UserId}, Duration={ElapsedMs}ms",
                            user.Id, stopwatch.ElapsedMilliseconds);

                        return Result<RefreshTokenResponse>.Failure("Invalid or expired refresh token");
                    }

                    // Check if user is active
                    if (!user.IsActive)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "RefreshToken FAILED: Account inactive - UserId={UserId}, Duration={ElapsedMs}ms",
                            user.Id, stopwatch.ElapsedMilliseconds);

                        return Result<RefreshTokenResponse>.Failure("Account is not active");
                    }

                    // Generate new access token
                    var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
                    if (!accessTokenResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            "RefreshToken FAILED: Access token generation failed - UserId={UserId}, Duration={ElapsedMs}ms",
                            user.Id, stopwatch.ElapsedMilliseconds);

                        return Result<RefreshTokenResponse>.Failure("Failed to generate access token");
                    }

                    // Generate new refresh token
                    var newRefreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();
                    if (!newRefreshTokenResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            "RefreshToken FAILED: Refresh token generation failed - UserId={UserId}, Duration={ElapsedMs}ms",
                            user.Id, stopwatch.ElapsedMilliseconds);

                        return Result<RefreshTokenResponse>.Failure("Failed to generate refresh token");
                    }

                    // Revoke old refresh token
                    var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress);
                    if (!revokeResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            "RefreshToken FAILED: Token revocation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            user.Id, revokeResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result<RefreshTokenResponse>.Failure(revokeResult.Error);
                    }

                    // Create and add new refresh token
                    var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenConfiguration.RefreshTokenExpirationDays);

                    var newRefreshToken = Domain.Users.ValueObjects.RefreshToken.Create(
                        newRefreshTokenResult.Value,
                        newRefreshTokenExpiry,
                        request.IpAddress ?? "unknown");

                    if (!newRefreshToken.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            "RefreshToken FAILED: Refresh token creation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            user.Id, newRefreshToken.Error, stopwatch.ElapsedMilliseconds);

                        return Result<RefreshTokenResponse>.Failure("Failed to create refresh token");
                    }

                    var addTokenResult = user.AddRefreshToken(newRefreshToken.Value);
                    if (!addTokenResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            "RefreshToken FAILED: Adding refresh token failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            user.Id, addTokenResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result<RefreshTokenResponse>.Failure(addTokenResult.Error);
                    }

                    // Save changes
                    await _unitOfWork.CommitAsync(cancellationToken);

                    // Calculate token expiry
                    var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationMinutes);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "RefreshToken COMPLETE: UserId={UserId}, IpAddress={IpAddress}, RefreshTokenExpiryDays={ExpiryDays}, Duration={ElapsedMs}ms",
                        user.Id, request.IpAddress ?? "unknown", _tokenConfiguration.RefreshTokenExpirationDays, stopwatch.ElapsedMilliseconds);

                    var response = new RefreshTokenResponse(
                        accessTokenResult.Value,
                        newRefreshTokenResult.Value,
                        tokenExpiresAt);

                    return Result<RefreshTokenResponse>.Success(response);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RefreshToken FAILED: Exception occurred - Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}