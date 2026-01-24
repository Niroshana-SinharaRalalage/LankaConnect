using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        LankaConnect.Domain.Common.IUnitOfWork unitOfWork,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateUser"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Email", request.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateUser START: Email={Email}, FirstName={FirstName}, LastName={LastName}",
                request.Email, request.FirstName, request.LastName);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create Email value object
                var emailResult = LankaConnect.Domain.Shared.ValueObjects.Email.Create(request.Email);
                if (emailResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateUser FAILED: Invalid email format - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.Email, string.Join(", ", emailResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(emailResult.Errors);
                }

                _logger.LogInformation(
                    "CreateUser: Email value object created - Email={Email}",
                    emailResult.Value.Value);

                // Check if user already exists
                var emailExists = await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken);
                if (emailExists)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateUser FAILED: Email already exists - Email={Email}, Duration={ElapsedMs}ms",
                        emailResult.Value.Value, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("A user with this email already exists");
                }

                _logger.LogInformation(
                    "CreateUser: Email uniqueness verified - Email={Email}",
                    emailResult.Value.Value);

                // Create PhoneNumber value object if provided
                LankaConnect.Domain.Shared.ValueObjects.PhoneNumber? phoneNumber = null;
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var phoneResult = LankaConnect.Domain.Shared.ValueObjects.PhoneNumber.Create(request.PhoneNumber);
                    if (phoneResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "CreateUser FAILED: Invalid phone number format - PhoneNumber={PhoneNumber}, Errors={Errors}, Duration={ElapsedMs}ms",
                            request.PhoneNumber, string.Join(", ", phoneResult.Errors), stopwatch.ElapsedMilliseconds);

                        return Result<Guid>.Failure(phoneResult.Errors);
                    }
                    phoneNumber = phoneResult.Value;

                    _logger.LogInformation(
                        "CreateUser: PhoneNumber value object created - PhoneNumber={PhoneNumber}",
                        phoneNumber.Value);
                }

                // Create User aggregate
                var userResult = User.Create(emailResult.Value, request.FirstName, request.LastName);
                if (userResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateUser FAILED: User creation failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        emailResult.Value.Value, string.Join(", ", userResult.Errors), stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(userResult.Errors);
                }

                var user = userResult.Value;

                _logger.LogInformation(
                    "CreateUser: User aggregate created - UserId={UserId}, Email={Email}",
                    user.Id, user.Email.Value);

                // Update profile if additional info provided
                if (phoneNumber != null || !string.IsNullOrEmpty(request.Bio))
                {
                    _logger.LogInformation(
                        "CreateUser: Updating profile with additional info - HasPhoneNumber={HasPhoneNumber}, HasBio={HasBio}",
                        phoneNumber != null, !string.IsNullOrEmpty(request.Bio));

                    var updateResult = user.UpdateProfile(
                        request.FirstName,
                        request.LastName,
                        phoneNumber,
                        request.Bio);

                    if (updateResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "CreateUser FAILED: Profile update failed - UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                            user.Id, string.Join(", ", updateResult.Errors), stopwatch.ElapsedMilliseconds);

                        return Result<Guid>.Failure(updateResult.Errors);
                    }

                    _logger.LogInformation(
                        "CreateUser: Profile updated successfully - UserId={UserId}",
                        user.Id);
                }

                // Save to repository
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateUser COMPLETE: UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    user.Id, user.Email.Value, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(user.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "CreateUser CANCELED: Operation was canceled - Email={Email}, Duration={ElapsedMs}ms",
                    request.Email, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateUser FAILED: Exception occurred - Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Email, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}