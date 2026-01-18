using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
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
        using (LogContext.PushProperty("Operation", "LogoutUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("IpAddress", request.IpAddress ?? "unknown"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("LogoutUser START: IpAddress={IpAddress}", request.IpAddress ?? "unknown");

            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LogoutUser VALIDATION FAILED: Refresh token missing - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Refresh token is required");
                }

                // Find user by refresh token
                var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LogoutUser FAILED: Invalid refresh token - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Invalid refresh token");
                }

                // Add UserId to LogContext now that we have it
                using (LogContext.PushProperty("UserId", user.Id))
                {
                    // Revoke the specific refresh token
                    var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress);
                    if (!revokeResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "LogoutUser FAILED: Token revocation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            user.Id, revokeResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result.Failure(revokeResult.Error);
                    }

                    // Invalidate the refresh token in the JWT service as well
                    await _jwtTokenService.InvalidateRefreshTokenAsync(request.RefreshToken);

                    // Save changes
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "LogoutUser COMPLETE: UserId={UserId}, IpAddress={IpAddress}, Duration={ElapsedMs}ms",
                        user.Id, request.IpAddress ?? "unknown", stopwatch.ElapsedMilliseconds);

                    return Result.Success();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "LogoutUser FAILED: Exception occurred - Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}