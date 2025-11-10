using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.SendEmailVerification;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
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
        try
        {
            // Create email value object
            var emailResult = LankaConnect.Domain.Shared.ValueObjects.Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                return Result<RegisterUserResponse>.Failure(emailResult.Error);
            }

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
            if (existingUser != null)
            {
                return Result<RegisterUserResponse>.Failure("A user with this email already exists");
            }

            // Validate and hash password
            var passwordValidationResult = _passwordHashingService.ValidatePasswordStrength(request.Password);
            if (!passwordValidationResult.IsSuccess)
            {
                return Result<RegisterUserResponse>.Failure(passwordValidationResult.Error);
            }

            var hashResult = _passwordHashingService.HashPassword(request.Password);
            if (!hashResult.IsSuccess)
            {
                return Result<RegisterUserResponse>.Failure(hashResult.Error);
            }

            // Create user
            var userResult = User.Create(emailResult.Value, request.FirstName, request.LastName, request.Role);
            if (!userResult.IsSuccess)
            {
                return Result<RegisterUserResponse>.Failure(userResult.Error);
            }

            var user = userResult.Value;

            // Set password
            var setPasswordResult = user.SetPassword(hashResult.Value);
            if (!setPasswordResult.IsSuccess)
            {
                return Result<RegisterUserResponse>.Failure(setPasswordResult.Error);
            }

            // Generate email verification token
            var verificationToken = Guid.NewGuid().ToString("N");
            var tokenExpiresAt = DateTime.UtcNow.AddHours(24);
            
            var setTokenResult = user.SetEmailVerificationToken(verificationToken, tokenExpiresAt);
            if (!setTokenResult.IsSuccess)
            {
                return Result<RegisterUserResponse>.Failure(setTokenResult.Error);
            }

            // Set preferred metro areas if provided (Phase 5A: Optional)
            if (request.PreferredMetroAreaIds != null && request.PreferredMetroAreaIds.Any())
            {
                var setMetroAreasResult = user.UpdatePreferredMetroAreas(request.PreferredMetroAreaIds);
                if (!setMetroAreasResult.IsSuccess)
                {
                    return Result<RegisterUserResponse>.Failure(setMetroAreasResult.Error);
                }
            }

            // Save user
            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Send verification email automatically
            var sendEmailCommand = new SendEmailVerificationCommand(user.Id);
            var sendEmailResult = await _mediator.Send(sendEmailCommand, cancellationToken);

            if (!sendEmailResult.IsSuccess)
            {
                _logger.LogWarning(
                    "User {UserId} registered successfully, but failed to send verification email: {Error}",
                    user.Id, sendEmailResult.Error);
            }

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            var response = new RegisterUserResponse(
                user.Id,
                user.Email.Value,
                user.FullName,
                EmailVerificationRequired: true);

            return Result<RegisterUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Email}", request.Email);
            return Result<RegisterUserResponse>.Failure("An error occurred during user registration");
        }
    }
}