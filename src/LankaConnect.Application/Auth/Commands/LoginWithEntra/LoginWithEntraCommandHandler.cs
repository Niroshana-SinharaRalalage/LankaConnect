using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Auth.Commands.LoginWithEntra;

/// <summary>
/// Handler for authenticating users via Microsoft Entra External ID
/// Implements auto-provisioning for new users
/// </summary>
public class LoginWithEntraCommandHandler : IRequestHandler<LoginWithEntraCommand, Result<LoginWithEntraResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEntraExternalIdService _entraService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenConfiguration _tokenConfiguration;
    private readonly ILogger<LoginWithEntraCommandHandler> _logger;

    public LoginWithEntraCommandHandler(
        IUserRepository userRepository,
        IEntraExternalIdService entraService,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ITokenConfiguration tokenConfiguration,
        ILogger<LoginWithEntraCommandHandler> logger)
    {
        _userRepository = userRepository;
        _entraService = entraService;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _tokenConfiguration = tokenConfiguration;
        _logger = logger;
    }

    public async Task<Result<LoginWithEntraResponse>> Handle(
        LoginWithEntraCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check if Entra External ID is enabled
        if (!_entraService.IsEnabled)
        {
            _logger.LogWarning("Entra External ID authentication attempt when service is disabled");
            return Result<LoginWithEntraResponse>.Failure("Entra External ID authentication is not enabled");
        }

        // 2. Validate Entra access token and get user info
        var userInfoResult = await _entraService.GetUserInfoAsync(request.AccessToken);
        if (userInfoResult.IsFailure)
        {
            _logger.LogWarning("Failed to validate Entra access token: {Errors}",
                string.Join(", ", userInfoResult.Errors));
            return Result<LoginWithEntraResponse>.Failure(userInfoResult.Errors);
        }

        var entraUserInfo = userInfoResult.Value;
        _logger.LogInformation("Successfully validated Entra token for user: {Email}", entraUserInfo.Email);

        // 3. Try to find existing user by external provider ID
        var existingUser = await _userRepository.GetByExternalProviderIdAsync(
            entraUserInfo.ObjectId,
            cancellationToken);

        User user;
        bool isNewUser = false;

        if (existingUser == null)
        {
            // 4. Auto-provision new user
            _logger.LogInformation("Auto-provisioning new user from Entra: {Email}", entraUserInfo.Email);

            var emailResult = Email.Create(entraUserInfo.Email);
            if (emailResult.IsFailure)
            {
                _logger.LogWarning("Invalid email from Entra token: {Email}", entraUserInfo.Email);
                return Result<LoginWithEntraResponse>.Failure(emailResult.Errors);
            }

            // Check if email already exists with different provider
            var emailExists = await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken);
            if (emailExists)
            {
                _logger.LogWarning("Email {Email} already registered with different provider", entraUserInfo.Email);
                return Result<LoginWithEntraResponse>.Failure(
                    $"An account with email {entraUserInfo.Email} is already registered with a different authentication method. Please use your original login method.");
            }

            // Create new user using external provider factory method
            var createUserResult = User.CreateFromExternalProvider(
                IdentityProvider.EntraExternal,
                entraUserInfo.ObjectId,
                emailResult.Value,
                entraUserInfo.FirstName,
                entraUserInfo.LastName,
                UserRole.User);

            if (createUserResult.IsFailure)
            {
                _logger.LogError("Failed to create user from Entra info: {Errors}",
                    string.Join(", ", createUserResult.Errors));
                return Result<LoginWithEntraResponse>.Failure(createUserResult.Errors);
            }

            user = createUserResult.Value;

            await _userRepository.AddAsync(user, cancellationToken);
            isNewUser = true;

            _logger.LogInformation("Successfully created new user {UserId} from Entra", user.Id);
        }
        else
        {
            user = existingUser;
            _logger.LogInformation("Found existing user {UserId} for Entra login", user.Id);
        }

        // 5. Generate JWT tokens
        var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
        if (accessTokenResult.IsFailure)
        {
            _logger.LogError("Failed to generate access token for user {UserId}: {Errors}",
                user.Id, string.Join(", ", accessTokenResult.Errors));
            return Result<LoginWithEntraResponse>.Failure(accessTokenResult.Errors);
        }

        var refreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();
        if (refreshTokenResult.IsFailure)
        {
            _logger.LogError("Failed to generate refresh token for user {UserId}: {Errors}",
                user.Id, string.Join(", ", refreshTokenResult.Errors));
            return Result<LoginWithEntraResponse>.Failure(refreshTokenResult.Errors);
        }

        // 6. Add refresh token to user
        var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationMinutes);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_tokenConfiguration.RefreshTokenExpirationDays);

        var refreshTokenObject = Domain.Users.ValueObjects.RefreshToken.Create(
            refreshTokenResult.Value,
            refreshTokenExpiresAt,
            request.IpAddress ?? "unknown");

        if (refreshTokenObject.IsFailure)
        {
            _logger.LogError("Failed to create refresh token for user {UserId}: {Errors}",
                user.Id, string.Join(", ", refreshTokenObject.Errors));
            return Result<LoginWithEntraResponse>.Failure(refreshTokenObject.Errors);
        }

        var addTokenResult = user.AddRefreshToken(refreshTokenObject.Value);

        if (addTokenResult.IsFailure)
        {
            _logger.LogError("Failed to add refresh token for user {UserId}: {Errors}",
                user.Id, string.Join(", ", addTokenResult.Errors));
            return Result<LoginWithEntraResponse>.Failure(addTokenResult.Errors);
        }

        // 7. Save changes
        var saveResult = await _unitOfWork.CommitAsync(cancellationToken);
        if (saveResult <= 0)
        {
            _logger.LogError("Failed to save user changes for {UserId}", user.Id);
            return Result<LoginWithEntraResponse>.Failure("Failed to save user authentication data");
        }

        _logger.LogInformation("User {UserId} successfully authenticated via Entra External ID", user.Id);

        // 8. Return success response
        var response = new LoginWithEntraResponse(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.Role,
            accessTokenResult.Value,
            refreshTokenResult.Value,
            tokenExpiresAt,
            isNewUser);

        return Result<LoginWithEntraResponse>.Success(response);
    }
}
