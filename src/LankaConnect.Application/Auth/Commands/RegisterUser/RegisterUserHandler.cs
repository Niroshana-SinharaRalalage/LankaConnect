using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.SendEmailVerification;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Auth.Commands.RegisterUser;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterUserHandler> _logger;
    private readonly IMediator _mediator;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        LankaConnect.Domain.Common.IUnitOfWork unitOfWork,
        ILogger<RegisterUserHandler> logger,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RegisterUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Email", request.Email))
        using (LogContext.PushProperty("SelectedRole", request.SelectedRole?.ToString() ?? "GeneralUser"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RegisterUser START: Email={Email}, SelectedRole={SelectedRole}, MetroAreasCount={MetroAreasCount}",
                request.Email, request.SelectedRole?.ToString() ?? "GeneralUser", request.PreferredMetroAreaIds?.Count ?? 0);

            try
            {
                // Create email value object
                var emailResult = LankaConnect.Domain.Shared.ValueObjects.Email.Create(request.Email);
                if (!emailResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegisterUser VALIDATION FAILED: Invalid email format - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email, stopwatch.ElapsedMilliseconds);

                    return Result<RegisterUserResponse>.Failure(emailResult.Error);
                }

                // Check if user already exists
                var existingUser = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
                if (existingUser != null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegisterUser FAILED: Email already exists - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email, stopwatch.ElapsedMilliseconds);

                    return Result<RegisterUserResponse>.Failure("A user with this email already exists");
                }

                // Validate and hash password
                var passwordValidationResult = _passwordHashingService.ValidatePasswordStrength(request.Password);
                if (passwordValidationResult == null || !passwordValidationResult.IsSuccess)
                {
                    stopwatch.Stop();

                    var error = passwordValidationResult?.Error ?? "Password validation failed";
                    _logger.LogWarning(
                        "RegisterUser VALIDATION FAILED: Weak password - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email, error, stopwatch.ElapsedMilliseconds);

                    return Result<RegisterUserResponse>.Failure(error);
                }

                var hashResult = _passwordHashingService.HashPassword(request.Password);
                if (hashResult == null || !hashResult.IsSuccess)
                {
                    stopwatch.Stop();

                    var error = hashResult?.Error ?? "Password hashing failed";
                    _logger.LogError(
                        "RegisterUser FAILED: Password hashing failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email, error, stopwatch.ElapsedMilliseconds);

                    return Result<RegisterUserResponse>.Failure(error);
                }

                // Phase 6A.0: Determine user role
                // If Event Organizer is selected, create as GeneralUser and set pending upgrade role
                // Admin will need to approve the upgrade request
                var selectedRole = request.SelectedRole ?? UserRole.GeneralUser;
                var actualRole = selectedRole == UserRole.EventOrganizer ? UserRole.GeneralUser : selectedRole;

                // Create user with actual role (GeneralUser if Event Organizer was selected)
                var userResult = User.Create(emailResult.Value, request.FirstName, request.LastName, actualRole);
                if (!userResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "RegisterUser FAILED: User creation failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email, userResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<RegisterUserResponse>.Failure(userResult.Error);
                }

                var user = userResult.Value;

                // Add UserId to LogContext now that we have it
                using (LogContext.PushProperty("UserId", user.Id))
                {
                    // Phase 6A.0: If Event Organizer selected, mark for pending upgrade
                    if (selectedRole == UserRole.EventOrganizer)
                    {
                        var setPendingRoleResult = user.SetPendingUpgradeRole(UserRole.EventOrganizer);
                        if (!setPendingRoleResult.IsSuccess)
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "RegisterUser FAILED: Setting pending role failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                                request.Email, user.Id, setPendingRoleResult.Error, stopwatch.ElapsedMilliseconds);

                            return Result<RegisterUserResponse>.Failure(setPendingRoleResult.Error);
                        }

                        _logger.LogInformation(
                            "RegisterUser: Pending upgrade role set - Email={Email}, UserId={UserId}, PendingRole=EventOrganizer",
                            request.Email, user.Id);
                        // Note: ApprovalRequest entity creation will be added in Phase 6A.5
                    }

                    // Set password
                    var setPasswordResult = user.SetPassword(hashResult.Value);
                    if (!setPasswordResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            "RegisterUser FAILED: Setting password failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.Email, user.Id, setPasswordResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result<RegisterUserResponse>.Failure(setPasswordResult.Error);
                    }

                    // Phase 6A.53: Generate email verification token (already done in User.Create())
                    // No need to call SetEmailVerificationToken - User.Create() now calls GenerateEmailVerificationToken()

                    // Set preferred metro areas if provided (Phase 5A: Optional)
                    if (request.PreferredMetroAreaIds != null && request.PreferredMetroAreaIds.Any())
                    {
                        var setMetroAreasResult = user.UpdatePreferredMetroAreas(request.PreferredMetroAreaIds);
                        if (!setMetroAreasResult.IsSuccess)
                        {
                            stopwatch.Stop();

                            _logger.LogWarning(
                                "RegisterUser: Metro areas update failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                                request.Email, user.Id, setMetroAreasResult.Error, stopwatch.ElapsedMilliseconds);

                            return Result<RegisterUserResponse>.Failure(setMetroAreasResult.Error);
                        }

                        _logger.LogInformation(
                            "RegisterUser: Metro areas set - Email={Email}, UserId={UserId}, Count={Count}",
                            request.Email, user.Id, request.PreferredMetroAreaIds.Count);
                    }

                    // Save user
                    await _userRepository.AddAsync(user, cancellationToken);

                    // Phase 6A.53: Email verification is sent automatically via domain event
                    // When CommitAsync() is called, it dispatches MemberVerificationRequestedEvent
                    // which triggers MemberVerificationRequestedEventHandler to send the email
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "RegisterUser COMPLETE: Email={Email}, UserId={UserId}, Role={Role}, PendingUpgrade={PendingUpgrade}, MetroAreasCount={MetroAreasCount}, Duration={ElapsedMs}ms",
                        request.Email, user.Id, user.Role, selectedRole == UserRole.EventOrganizer, request.PreferredMetroAreaIds?.Count ?? 0, stopwatch.ElapsedMilliseconds);

                    var response = new RegisterUserResponse(
                        user.Id,
                        user.Email.Value,
                        user.FullName,
                        EmailVerificationRequired: true);

                    return Result<RegisterUserResponse>.Success(response);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RegisterUser FAILED: Exception occurred - Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Email, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}