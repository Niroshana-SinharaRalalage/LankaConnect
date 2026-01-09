using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;

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
        try
        {
            _logger.LogInformation("Updating email for user {UserId} to {NewEmail}",
                request.UserId, request.NewEmail);

            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID {UserId}", request.UserId);
                return Result<UpdateUserEmailResponse>.Failure("User not found");
            }

            // Check if email is same as current
            if (user.Email.Value.Equals(request.NewEmail, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Email unchanged for user {UserId}", request.UserId);
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
                _logger.LogWarning("Invalid email format for user {UserId}: {Error}",
                    request.UserId, emailResult.Errors.FirstOrDefault());
                return Result<UpdateUserEmailResponse>.Failure(emailResult.Errors.FirstOrDefault() ?? "Invalid email format");
            }

            var newEmail = emailResult.Value;

            // Check if email is already in use by another user
            var existingUser = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
            if (existingUser != null && existingUser.Id != request.UserId)
            {
                _logger.LogWarning("Email {Email} is already in use by another user", request.NewEmail);
                return Result<UpdateUserEmailResponse>.Failure("Email is already in use by another account");
            }

            // Update email using domain method
            var changeEmailResult = user.ChangeEmail(newEmail);
            if (changeEmailResult.IsFailure)
            {
                _logger.LogWarning("Failed to change email for user {UserId}: {Error}",
                    request.UserId, changeEmailResult.Errors.FirstOrDefault());
                return Result<UpdateUserEmailResponse>.Failure(changeEmailResult.Errors.FirstOrDefault() ?? "Failed to change email");
            }

            // Phase 6A.70: Generate verification token (triggers domain event)
            // This will send verification email automatically via MemberVerificationRequestedEvent handler
            user.GenerateEmailVerificationToken();

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Email updated successfully for user {UserId}. Verification email sent to {NewEmail}",
                request.UserId, request.NewEmail);

            // Return response
            return Result<UpdateUserEmailResponse>.Success(new UpdateUserEmailResponse
            {
                Email = user.Email.Value,
                IsVerified = user.IsEmailVerified,
                VerificationSentAt = DateTime.UtcNow,
                Message = $"Email updated to {user.Email.Value}. Verification email sent. Please check your inbox."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email for user {UserId}", request.UserId);
            return Result<UpdateUserEmailResponse>.Failure("An error occurred while updating email");
        }
    }
}
