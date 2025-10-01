using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.SendEmailVerification;

/// <summary>
/// Handler for sending email verification emails
/// </summary>
public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand, Result<SendEmailVerificationResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendEmailVerificationCommandHandler> _logger;

    public SendEmailVerificationCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IUnitOfWork unitOfWork,
        ILogger<SendEmailVerificationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SendEmailVerificationResponse>> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<SendEmailVerificationResponse>.Failure("User not found");
            }

            // Check if email is already verified
            if (user.IsEmailVerified && !request.ForceResend)
            {
                return Result<SendEmailVerificationResponse>.Failure("Email is already verified");
            }

            // Use provided email or user's current email
            var targetEmail = request.Email ?? user.Email.Value;

            // Check if recently sent (within last 5 minutes) unless forcing resend
            if (!request.ForceResend && user.EmailVerificationTokenExpiresAt.HasValue)
            {
                var tokenCreatedAt = user.EmailVerificationTokenExpiresAt.Value.AddHours(-24); // Tokens expire after 24 hours
                if (DateTime.UtcNow.Subtract(tokenCreatedAt).TotalMinutes < 5)
                {
                    var response = new SendEmailVerificationResponse(
                        user.Id,
                        targetEmail,
                        user.EmailVerificationTokenExpiresAt.Value,
                        wasRecentlySent: true);

                    return Result<SendEmailVerificationResponse>.Success(response);
                }
            }

            // Generate new verification token
            var verificationToken = Guid.NewGuid().ToString("N");
            var tokenExpiresAt = DateTime.UtcNow.AddHours(24);

            // Set the token on user
            var setTokenResult = user.SetEmailVerificationToken(verificationToken, tokenExpiresAt);
            if (!setTokenResult.IsSuccess)
            {
                return Result<SendEmailVerificationResponse>.Failure(setTokenResult.Error);
            }

            // Prepare template parameters
            var templateParameters = new Dictionary<string, object>
            {
                { "UserName", user.FullName },
                { "UserEmail", targetEmail },
                { "VerificationToken", verificationToken },
                { "VerificationLink", $"https://lankaconnect.com/verify-email?token={verificationToken}" },
                { "ExpiresAt", tokenExpiresAt.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                { "CompanyName", "LankaConnect" }
            };

            // Send verification email
            var sendResult = await _emailService.SendTemplatedEmailAsync(
                "email-verification",
                targetEmail,
                templateParameters,
                cancellationToken);

            if (!sendResult.IsSuccess)
            {
                _logger.LogError("Failed to send verification email to {Email}: {Error}", 
                    targetEmail, sendResult.Error);
                return Result<SendEmailVerificationResponse>.Failure("Failed to send verification email");
            }

            // Save user with new token
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Verification email sent successfully to {Email} for user {UserId}", 
                targetEmail, user.Id);

            var successResponse = new SendEmailVerificationResponse(
                user.Id,
                targetEmail,
                tokenExpiresAt);

            return Result<SendEmailVerificationResponse>.Success(successResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email for user {UserId}", request.UserId);
            return Result<SendEmailVerificationResponse>.Failure("An error occurred while sending verification email");
        }
    }
}