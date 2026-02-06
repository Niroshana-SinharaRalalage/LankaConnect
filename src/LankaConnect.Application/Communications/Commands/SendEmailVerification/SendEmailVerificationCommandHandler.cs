using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.SendEmailVerification;

/// <summary>
/// Handler for sending email verification emails
/// Phase 6A.53: Generate verification token via domain event
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "SendEmailVerification"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendEmailVerification START: UserId={UserId}, ForceResend={ForceResend}",
                request.UserId,
                request.ForceResend);

            try
            {
                // Get user
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendEmailVerification FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendEmailVerificationResponse>.Failure("User not found");
                }

                // Check if email is already verified
                // FIX: Return SUCCESS (not failure) when email is already verified
                // This prevents 400 Bad Request errors on the frontend
                if (user.IsEmailVerified && !request.ForceResend)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "SendEmailVerification: Email already verified - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                        user.Id,
                        user.Email.Value,
                        stopwatch.ElapsedMilliseconds);

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
                        stopwatch.Stop();
                        _logger.LogInformation(
                            "SendEmailVerification: Recently sent, skipping resend - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                            user.Id,
                            targetEmail,
                            stopwatch.ElapsedMilliseconds);

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

                _logger.LogInformation(
                    "SendEmailVerification: Verification token generated - UserId={UserId}, Email={Email}",
                    user.Id,
                    targetEmail);

                // Save user with new token (domain event will be dispatched and email sent automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                var successResponse = new SendEmailVerificationResponse(
                    user.Id,
                    targetEmail,
                    user.EmailVerificationTokenExpiresAt ?? DateTime.UtcNow.AddHours(24));

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendEmailVerification COMPLETE: UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    user.Id,
                    targetEmail,
                    stopwatch.ElapsedMilliseconds);

                return Result<SendEmailVerificationResponse>.Success(successResponse);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SendEmailVerification FAILED: Unexpected error - UserId={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}