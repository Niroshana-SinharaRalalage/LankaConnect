using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Auth.Commands.LoginUser;

public class LoginUserHandler : IRequestHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;
    private readonly ITokenConfiguration _tokenConfiguration;
    private readonly ILogger<LoginUserHandler> _logger;

    public LoginUserHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService,
        LankaConnect.Domain.Common.IUnitOfWork unitOfWork,
        ITokenConfiguration tokenConfiguration,
        ILogger<LoginUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _tokenConfiguration = tokenConfiguration;
        _logger = logger;
    }

    public async Task<Result<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "LoginUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Email", request.Email))
        using (LogContext.PushProperty("RememberMe", request.RememberMe))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "LoginUser START: Email={Email}, RememberMe={RememberMe}, IpAddress={IpAddress}",
                request.Email, request.RememberMe, request.IpAddress ?? "unknown");

            try
            {
                // Create email value object
                var emailResult = Email.Create(request.Email);
                if (!emailResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginUser VALIDATION FAILED: Invalid email format - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Invalid email format");
                }

                // Find user
                var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginUser FAILED: User not found - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Invalid email or password");
                }

                // Check if account is locked
                if (user.IsAccountLocked)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginUser FAILED: Account locked - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Account is temporarily locked due to multiple failed login attempts");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginUser FAILED: Account inactive - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Account is not active");
                }

                // Verify password
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "LoginUser FAILED: Missing password hash - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Invalid email or password");
                }

                var passwordResult = _passwordHashingService.VerifyPassword(request.Password, user.PasswordHash);
                if (!passwordResult.IsSuccess || !passwordResult.Value)
                {
                    // Record failed login attempt
                    user.RecordFailedLoginAttempt();
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginUser FAILED: Invalid password - Email={Email}, UserId={UserId}, FailedAttempts={FailedAttempts}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, user.FailedLoginAttempts, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Invalid email or password");
                }

                // Check email verification requirement
                if (!user.IsEmailVerified)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginUser FAILED: Email not verified - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Email address must be verified before logging in");
                }

                // Generate tokens
                var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
                if (!accessTokenResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "LoginUser FAILED: Access token generation failed - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Failed to generate access token");
                }

                var refreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();
                if (!refreshTokenResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "LoginUser FAILED: Refresh token generation failed - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Failed to generate refresh token");
                }

                // Create and add refresh token
                // Phase AUTH-IMPROVEMENT: Support "Remember Me" functionality
                // - RememberMe = true: 30 days (long-lived session like Facebook/Gmail)
                // - RememberMe = false: 7 days (standard session)
                var refreshTokenDays = request.RememberMe ? 30 : 7;
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenDays);

                var refreshToken = Domain.Users.ValueObjects.RefreshToken.Create(
                    refreshTokenResult.Value,
                    refreshTokenExpiry,
                    request.IpAddress ?? "unknown");

                if (!refreshToken.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "LoginUser FAILED: Refresh token creation failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, refreshToken.Error, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure("Failed to create refresh token");
                }

                var addTokenResult = user.AddRefreshToken(refreshToken.Value);
                if (!addTokenResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "LoginUser FAILED: Adding refresh token failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, addTokenResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<LoginUserResponse>.Failure(addTokenResult.Error);
                }

                // Record successful login
                user.RecordSuccessfulLogin();
                await _unitOfWork.CommitAsync(cancellationToken);

                // Calculate token expiry
                var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationMinutes);

                stopwatch.Stop();

                _logger.LogInformation(
                    "LoginUser COMPLETE: Email={Email}, UserId={UserId}, Role={Role}, RememberMe={RememberMe}, RefreshTokenDays={RefreshTokenDays}, Duration={ElapsedMs}ms",
                    request.Email, user.Id, user.Role, request.RememberMe, refreshTokenDays, stopwatch.ElapsedMilliseconds);

                var response = new LoginUserResponse(
                    user.Id,
                    user.Email.Value,
                    user.FullName,
                    user.PhoneNumber?.Value,    // Phase 6A.X: Include phone number for organizer contact auto-population
                    user.Role,
                    accessTokenResult.Value,
                    refreshTokenResult.Value,
                    tokenExpiresAt,
                    user.IsEmailVerified,       // FIX: Include email verification status for UI
                    user.PendingUpgradeRole,    // Phase 6A.7: Include pending role for UI display
                    user.UpgradeRequestedAt,    // Phase 6A.7: Include when upgrade was requested
                    user.ProfilePhotoUrl);      // Include profile photo URL for header display

                return Result<LoginUserResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "LoginUser FAILED: Exception occurred - Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Email, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}