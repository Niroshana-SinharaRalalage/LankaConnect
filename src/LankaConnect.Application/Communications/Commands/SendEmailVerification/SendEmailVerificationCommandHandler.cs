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
            // FIX: Return SUCCESS (not failure) when email is already verified
            // This prevents 400 Bad Request errors on the frontend
            if (user.IsEmailVerified && !request.ForceResend)
            {
                _logger.LogInformation("Email is already verified for user {UserId}, returning success", user.Id);

                var alreadyVerifiedResponse = new SendEmailVerificationResponse(
                    user.Id,
                    user.Email.Value,
                    user.EmailVerificationTokenExpiresAt ?? DateTime.UtcNow,
                    wasRecentlySent: false);

                return Result<SendEmailVerificationResponse>.Success(alreadyVerifiedResponse);
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

            // Phase 6A.53: Generate new verification token (triggers MemberVerificationRequestedEvent)
            user.GenerateEmailVerificationToken();

            // Save user with new token (domain event will be dispatched and email sent automatically)
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Verification token generated for user {UserId}, email will be sent via event handler",
                user.Id);

            var successResponse = new SendEmailVerificationResponse(
                user.Id,
                targetEmail,
                user.EmailVerificationTokenExpiresAt ?? DateTime.UtcNow.AddHours(24));

            return Result<SendEmailVerificationResponse>.Success(successResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email for user {UserId}", request.UserId);
            return Result<SendEmailVerificationResponse>.Failure("An error occurred while sending verification email");
        }
    }
}