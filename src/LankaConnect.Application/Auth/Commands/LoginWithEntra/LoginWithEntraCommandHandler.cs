using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using RefreshTokenVO = LankaConnect.Domain.Users.ValueObjects.RefreshToken;
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
        using (LogContext.PushProperty("Operation", "LoginWithEntra"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("IpAddress", request.IpAddress ?? "unknown"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("LoginWithEntra START: IpAddress={IpAddress}", request.IpAddress ?? "unknown");

            try
            {
                // 1. Check if Entra External ID is enabled
                if (!_entraService.IsEnabled)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginWithEntra FAILED: Service disabled - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result<LoginWithEntraResponse>.Failure("Entra External ID authentication is not enabled");
                }

                // 2. Validate Entra access token and get user info
                var userInfoResult = await _entraService.GetUserInfoAsync(request.AccessToken);
                if (userInfoResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "LoginWithEntra FAILED: Invalid access token - Errors={Errors}, Duration={ElapsedMs}ms",
                        string.Join(", ", userInfoResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result<LoginWithEntraResponse>.Failure(userInfoResult.Errors);
                }

                var entraUserInfo = userInfoResult.Value;

                // Add email to LogContext now that we have it
                using (LogContext.PushProperty("Email", entraUserInfo.Email))
                {
                    _logger.LogInformation("LoginWithEntra: Token validated - Email={Email}", entraUserInfo.Email);

                    // Epic 1 Phase 2: Parse federated provider from idp claim
                    var federatedProviderResult = FederatedProviderExtensions.FromIdpClaimValue(entraUserInfo.IdentityProvider);
                    if (federatedProviderResult.IsFailure)
                    {
                        // Default to Microsoft if idp claim is missing or invalid
                        _logger.LogWarning(
                            "LoginWithEntra: Could not parse federated provider from idp claim '{IdpClaim}', defaulting to Microsoft",
                            entraUserInfo.IdentityProvider);
                        federatedProviderResult = Result<FederatedProvider>.Success(FederatedProvider.Microsoft);
                    }

                    var federatedProvider = federatedProviderResult.Value;
                    _logger.LogInformation("LoginWithEntra: Federated provider detected - Email={Email}, Provider={Provider}",
                        entraUserInfo.Email, federatedProvider.ToDisplayName());

                    // 3. Try to find existing user by external provider ID
                    var existingUser = await _userRepository.GetByExternalProviderIdAsync(
                        entraUserInfo.ObjectId,
                        cancellationToken);

                    User user;
                    bool isNewUser = false;

                    if (existingUser == null)
                    {
                        // 4. Auto-provision new user
                        _logger.LogInformation("LoginWithEntra: Auto-provisioning new user - Email={Email}", entraUserInfo.Email);

                        var emailResult = Email.Create(entraUserInfo.Email);
                        if (emailResult.IsFailure)
                        {
                            stopwatch.Stop();

                            _logger.LogWarning(
                                "LoginWithEntra FAILED: Invalid email from Entra - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, string.Join(", ", emailResult.Errors), stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure(emailResult.Errors);
                        }

                        // Check if email already exists with different provider
                        var emailExists = await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken);
                        if (emailExists)
                        {
                            stopwatch.Stop();

                            _logger.LogWarning(
                                "LoginWithEntra FAILED: Email already registered with different provider - Email={Email}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure(
                                $"An account with email {entraUserInfo.Email} is already registered with a different authentication method. Please use your original login method.");
                        }

                        // Create new user using external provider factory method
                        // Epic 1 Phase 2: Pass federatedProvider to automatically link external login
                        var createUserResult = User.CreateFromExternalProvider(
                            IdentityProvider.EntraExternal,
                            entraUserInfo.ObjectId,
                            emailResult.Value,
                            entraUserInfo.FirstName,
                            entraUserInfo.LastName,
                            federatedProvider,
                            entraUserInfo.Email, // Provider email
                            UserRole.GeneralUser);

                        if (createUserResult.IsFailure)
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "LoginWithEntra FAILED: User creation failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, string.Join(", ", createUserResult.Errors), stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure(createUserResult.Errors);
                        }

                        user = createUserResult.Value;

                        await _userRepository.AddAsync(user, cancellationToken);
                        isNewUser = true;

                        _logger.LogInformation("LoginWithEntra: New user created - Email={Email}, UserId={UserId}", entraUserInfo.Email, user.Id);
                    }
                    else
                    {
                        user = existingUser;
                        _logger.LogInformation("LoginWithEntra: Existing user found - Email={Email}, UserId={UserId}", entraUserInfo.Email, user.Id);

                        // Opportunistic profile sync on login (if data changed in Entra)
                        if (user.FirstName != entraUserInfo.FirstName || user.LastName != entraUserInfo.LastName)
                        {
                            _logger.LogInformation(
                                "LoginWithEntra: Syncing profile changes - UserId={UserId}, FirstName: {OldFirst}→{NewFirst}, LastName: {OldLast}→{NewLast}",
                                user.Id, user.FirstName, entraUserInfo.FirstName, user.LastName, entraUserInfo.LastName);

                            var updateResult = user.UpdateProfile(
                                entraUserInfo.FirstName,
                                entraUserInfo.LastName,
                                user.PhoneNumber, // Keep existing phone
                                user.Bio);        // Keep existing bio

                            if (updateResult.IsFailure)
                            {
                                // Log but don't fail login - authentication succeeded
                                _logger.LogWarning(
                                    "LoginWithEntra: Profile sync failed - UserId={UserId}, Errors={Errors}. Login will proceed.",
                                    user.Id, string.Join(", ", updateResult.Errors));
                            }
                            else
                            {
                                _logger.LogInformation("LoginWithEntra: Profile synced successfully - UserId={UserId}", user.Id);
                            }
                        }
                    }

                    // Add UserId to LogContext now that we have it
                    using (LogContext.PushProperty("UserId", user.Id))
                    {
                        // 5. Generate JWT tokens
                        var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
                        if (accessTokenResult.IsFailure)
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "LoginWithEntra FAILED: Access token generation failed - Email={Email}, UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, user.Id, string.Join(", ", accessTokenResult.Errors), stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure(accessTokenResult.Errors);
                        }

                        var refreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();
                        if (refreshTokenResult.IsFailure)
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "LoginWithEntra FAILED: Refresh token generation failed - Email={Email}, UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, user.Id, string.Join(", ", refreshTokenResult.Errors), stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure(refreshTokenResult.Errors);
                        }

                        // 6. Add refresh token to user
                        var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationMinutes);
                        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_tokenConfiguration.RefreshTokenExpirationDays);

                        var refreshTokenObject = RefreshTokenVO.Create(
                            refreshTokenResult.Value,
                            refreshTokenExpiresAt,
                            request.IpAddress ?? "unknown");

                        if (refreshTokenObject.IsFailure)
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "LoginWithEntra FAILED: Refresh token creation failed - Email={Email}, UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, user.Id, string.Join(", ", refreshTokenObject.Errors), stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure(refreshTokenObject.Errors);
                        }

                        var addTokenResult = user.AddRefreshToken(refreshTokenObject.Value);

                        if (addTokenResult.IsFailure)
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "LoginWithEntra FAILED: Adding refresh token failed - Email={Email}, UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, user.Id, string.Join(", ", addTokenResult.Errors), stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure(addTokenResult.Errors);
                        }

                        // 7. Save changes
                        var saveResult = await _unitOfWork.CommitAsync(cancellationToken);
                        if (saveResult <= 0)
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "LoginWithEntra FAILED: Save failed - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                                entraUserInfo.Email, user.Id, stopwatch.ElapsedMilliseconds);

                            return Result<LoginWithEntraResponse>.Failure("Failed to save user authentication data");
                        }

                        stopwatch.Stop();

                        _logger.LogInformation(
                            "LoginWithEntra COMPLETE: Email={Email}, UserId={UserId}, Role={Role}, IsNewUser={IsNewUser}, Provider={Provider}, Duration={ElapsedMs}ms",
                            entraUserInfo.Email, user.Id, user.Role, isNewUser, federatedProvider.ToDisplayName(), stopwatch.ElapsedMilliseconds);

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
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "LoginWithEntra FAILED: Exception occurred - Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
