using MediatR;
using Microsoft.Extensions.Logging;
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
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<RefreshTokenResponse>.Failure("Refresh token is required");
            }

            // Find user by refresh token
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Refresh token not found or user not found");
                return Result<RefreshTokenResponse>.Failure("Invalid refresh token");
            }

            // Get the refresh token
            var refreshToken = user.GetRefreshToken(request.RefreshToken);
            if (refreshToken == null || !refreshToken.IsActive)
            {
                _logger.LogWarning("Refresh token is invalid or expired for user: {UserId}", user.Id);
                return Result<RefreshTokenResponse>.Failure("Invalid or expired refresh token");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Refresh token attempt on inactive account: {UserId}", user.Id);
                return Result<RefreshTokenResponse>.Failure("Account is not active");
            }

            // Generate new access token
            var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
            if (!accessTokenResult.IsSuccess)
            {
                _logger.LogError("Failed to generate new access token for user: {UserId}", user.Id);
                return Result<RefreshTokenResponse>.Failure("Failed to generate access token");
            }

            // Generate new refresh token
            var newRefreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();
            if (!newRefreshTokenResult.IsSuccess)
            {
                _logger.LogError("Failed to generate new refresh token for user: {UserId}", user.Id);
                return Result<RefreshTokenResponse>.Failure("Failed to generate refresh token");
            }

            // Revoke old refresh token
            var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress);
            if (!revokeResult.IsSuccess)
            {
                _logger.LogError("Failed to revoke old refresh token for user: {UserId}", user.Id);
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
                _logger.LogError("Failed to create new refresh token for user: {UserId}", user.Id);
                return Result<RefreshTokenResponse>.Failure("Failed to create refresh token");
            }

            var addTokenResult = user.AddRefreshToken(newRefreshToken.Value);
            if (!addTokenResult.IsSuccess)
            {
                _logger.LogError("Failed to add new refresh token for user: {UserId}", user.Id);
                return Result<RefreshTokenResponse>.Failure(addTokenResult.Error);
            }

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            // Calculate token expiry
            var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationMinutes);

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

            var response = new RefreshTokenResponse(
                accessTokenResult.Value,
                newRefreshTokenResult.Value,
                tokenExpiresAt);

            return Result<RefreshTokenResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Result<RefreshTokenResponse>.Failure("An error occurred during token refresh");
        }
    }
}