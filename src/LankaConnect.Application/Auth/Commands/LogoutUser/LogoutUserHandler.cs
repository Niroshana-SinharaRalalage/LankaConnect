using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;
using DomainUserRepository = LankaConnect.Domain.Users.IUserRepository;
using DomainUnitOfWork = LankaConnect.Domain.Common.IUnitOfWork;

namespace LankaConnect.Application.Auth.Commands.LogoutUser;

public class LogoutUserHandler : IRequestHandler<LogoutUserCommand, Result>
{
    private readonly DomainUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly DomainUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutUserHandler> _logger;

    public LogoutUserHandler(
        DomainUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        DomainUnitOfWork unitOfWork,
        ILogger<LogoutUserHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result.Failure("Refresh token is required");
            }

            // Find user by refresh token
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Logout attempt with invalid refresh token");
                return Result.Failure("Invalid refresh token");
            }

            // Revoke the specific refresh token
            var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress);
            if (!revokeResult.IsSuccess)
            {
                _logger.LogWarning("Failed to revoke refresh token for user: {UserId}", user.Id);
                return Result.Failure(revokeResult.Error);
            }

            // Invalidate the refresh token in the JWT service as well
            await _jwtTokenService.InvalidateRefreshTokenAsync(request.RefreshToken);

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("User logged out successfully: {UserId}", user.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Result.Failure("An error occurred during logout");
        }
    }
}