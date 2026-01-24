using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Serilog.Context;

namespace LankaConnect.Application.Users.Commands.UpdateUserEmail;

/// <summary>
/// Handler for updating user's email address
/// Phase 6A.70: Profile Basic Info Section with Email Verification
///
/// Process:
/// 1. Validate new email format
/// 2. Check uniqueness (email not already in use)
/// 3. Update user email
/// 4. Set IsEmailVerified = false
/// 5. Generate verification token (triggers MemberVerificationRequestedEvent)
/// 6. Email sent automatically via domain event handler
/// </summary>
public class UpdateUserEmailCommandHandler : IRequestHandler<UpdateUserEmailCommand, Result<UpdateUserEmailResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserEmailCommandHandler> _logger;

    public UpdateUserEmailCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UpdateUserEmailResponse>> Handle(UpdateUserEmailCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateUserEmail"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        using (LogContext.PushProperty("NewEmail", request.NewEmail))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateUserEmail START: UserId={UserId}, NewEmail={NewEmail}",
                request.UserId, request.NewEmail);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateUserEmail FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<UpdateUserEmailResponse>.Failure("User not found");
                }

                _logger.LogInformation(
                    "UpdateUserEmail: User loaded - UserId={UserId}, CurrentEmail={CurrentEmail}, IsVerified={IsVerified}",
                    user.Id, user.Email.Value, user.IsEmailVerified);

                // Check if email is same as current
                if (user.Email.Value.Equals(request.NewEmail, StringComparison.OrdinalIgnoreCase))
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "UpdateUserEmail COMPLETE: Email unchanged - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<UpdateUserEmailResponse>.Success(new UpdateUserEmailResponse
                    {
                        Email = user.Email.Value,
                        IsVerified = user.IsEmailVerified,
                        VerificationSentAt = null,
                        Message = "Email unchanged"
                    });
                }

                // Parse and validate new email
                var emailResult = Email.Create(request.NewEmail);
                if (emailResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateUserEmail FAILED: Invalid email format - UserId={UserId}, NewEmail={NewEmail}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, request.NewEmail, emailResult.Errors.FirstOrDefault(), stopwatch.ElapsedMilliseconds);

                    return Result<UpdateUserEmailResponse>.Failure(emailResult.Errors.FirstOrDefault() ?? "Invalid email format");
                }

                var newEmail = emailResult.Value;

                _logger.LogInformation(
                    "UpdateUserEmail: Email format validated - NewEmail={NewEmail}",
                    newEmail.Value);

                // Check if email is already in use by another user
                var existingUser = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
                if (existingUser != null && existingUser.Id != request.UserId)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateUserEmail FAILED: Email already in use - UserId={UserId}, NewEmail={NewEmail}, ExistingUserId={ExistingUserId}, Duration={ElapsedMs}ms",
                        request.UserId, request.NewEmail, existingUser.Id, stopwatch.ElapsedMilliseconds);

                    return Result<UpdateUserEmailResponse>.Failure("Email is already in use by another account");
                }

                _logger.LogInformation(
                    "UpdateUserEmail: Email uniqueness verified - NewEmail={NewEmail}",
                    newEmail.Value);

                // Update email using domain method
                var changeEmailResult = user.ChangeEmail(newEmail);
                if (changeEmailResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateUserEmail FAILED: Domain validation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, changeEmailResult.Errors.FirstOrDefault(), stopwatch.ElapsedMilliseconds);

                    return Result<UpdateUserEmailResponse>.Failure(changeEmailResult.Errors.FirstOrDefault() ?? "Failed to change email");
                }

                _logger.LogInformation(
                    "UpdateUserEmail: Domain method succeeded - UserId={UserId}, NewEmail={NewEmail}",
                    user.Id, newEmail.Value);

                // Phase 6A.70: Generate verification token (triggers domain event)
                // This will send verification email automatically via MemberVerificationRequestedEvent handler
                user.GenerateEmailVerificationToken();

                _logger.LogInformation(
                    "UpdateUserEmail: Email verification token generated - UserId={UserId}",
                    user.Id);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateUserEmail COMPLETE: Email updated and verification sent - UserId={UserId}, NewEmail={NewEmail}, Duration={ElapsedMs}ms",
                    request.UserId, request.NewEmail, stopwatch.ElapsedMilliseconds);

                // Return response
                return Result<UpdateUserEmailResponse>.Success(new UpdateUserEmailResponse
                {
                    Email = user.Email.Value,
                    IsVerified = user.IsEmailVerified,
                    VerificationSentAt = DateTime.UtcNow,
                    Message = $"Email updated to {user.Email.Value}. Verification email sent. Please check your inbox."
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "UpdateUserEmail CANCELED: Operation was canceled - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateUserEmail FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<UpdateUserEmailResponse>.Failure("An error occurred while updating email");
            }
        }
    }
}
