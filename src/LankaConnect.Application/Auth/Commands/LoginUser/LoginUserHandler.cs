using MediatR;
using Microsoft.Extensions.Logging;
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
        try
        {
            // Create email value object
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                return Result<LoginUserResponse>.Failure("Invalid email format");
            }

            // Find user
            var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Invalid email or password");
            }

            // Check if account is locked
            if (user.IsAccountLocked)
            {
                _logger.LogWarning("Login attempt on locked account: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Account is temporarily locked due to multiple failed login attempts");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt on inactive account: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Account is not active");
            }

            // Verify password
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogError("User {Email} has no password hash", request.Email);
                return Result<LoginUserResponse>.Failure("Invalid email or password");
            }

            var passwordResult = _passwordHashingService.VerifyPassword(request.Password, user.PasswordHash);
            if (!passwordResult.IsSuccess || !passwordResult.Value)
            {
                // Record failed login attempt
                user.RecordFailedLoginAttempt();
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogWarning("Failed login attempt for: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Invalid email or password");
            }

            // Check email verification requirement
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login attempt with unverified email: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Email address must be verified before logging in");
            }

            // Generate tokens
            var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
            if (!accessTokenResult.IsSuccess)
            {
                _logger.LogError("Failed to generate access token for user: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Failed to generate access token");
            }

            var refreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();
            if (!refreshTokenResult.IsSuccess)
            {
                _logger.LogError("Failed to generate refresh token for user: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Failed to generate refresh token");
            }

            // Create and add refresh token
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenConfiguration.RefreshTokenExpirationDays);
            
            var refreshToken = Domain.Users.ValueObjects.RefreshToken.Create(
                refreshTokenResult.Value, 
                refreshTokenExpiry, 
                request.IpAddress ?? "unknown");
            
            if (!refreshToken.IsSuccess)
            {
                _logger.LogError("Failed to create refresh token for user: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure("Failed to create refresh token");
            }

            var addTokenResult = user.AddRefreshToken(refreshToken.Value);
            if (!addTokenResult.IsSuccess)
            {
                _logger.LogError("Failed to add refresh token for user: {Email}", request.Email);
                return Result<LoginUserResponse>.Failure(addTokenResult.Error);
            }

            // Record successful login
            user.RecordSuccessfulLogin();
            await _unitOfWork.CommitAsync(cancellationToken);

            // Calculate token expiry
            var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationMinutes);

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            var response = new LoginUserResponse(
                user.Id,
                user.Email.Value,
                user.FullName,
                user.Role,
                accessTokenResult.Value,
                refreshTokenResult.Value,
                tokenExpiresAt);

            return Result<LoginUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for: {Email}", request.Email);
            return Result<LoginUserResponse>.Failure("An error occurred during login");
        }
    }
}