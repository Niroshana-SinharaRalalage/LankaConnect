using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Users.EventHandlers;

/// <summary>
/// Handles MemberVerificationRequestedEvent to send email verification link.
/// Phase 6A.53: Member Email Verification System
/// Uses fail-silent pattern to prevent transaction rollback.
/// </summary>
public class MemberVerificationRequestedEventHandler
    : INotificationHandler<DomainEventNotification<MemberVerificationRequestedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<MemberVerificationRequestedEventHandler> _logger;
    private readonly IApplicationUrlsService _urlsService;

    public MemberVerificationRequestedEventHandler(
        IEmailService emailService,
        ILogger<MemberVerificationRequestedEventHandler> logger,
        IApplicationUrlsService urlsService)
    {
        _emailService = emailService;
        _logger = logger;
        _urlsService = urlsService;
    }

    public async Task Handle(
        DomainEventNotification<MemberVerificationRequestedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "[Phase 6A.53] Handling MemberVerificationRequestedEvent for User {UserId}",
            domainEvent.UserId);

        try
        {
            // Generate verification URL
            var verificationUrl = _urlsService.GetEmailVerificationUrl(domainEvent.VerificationToken);

            var parameters = new Dictionary<string, object>
            {
                { "Email", domainEvent.Email },
                { "VerificationUrl", verificationUrl },
                { "ExpirationHours", 24 }
            };

            _logger.LogInformation(
                "[Phase 6A.53] Sending member-email-verification to {Email}, VerificationUrl: {VerificationUrl}",
                domainEvent.Email, verificationUrl);

            var result = await _emailService.SendTemplatedEmailAsync(
                "member-email-verification",
                domainEvent.Email,
                parameters,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "[Phase 6A.53] Failed to send verification email to {Email}: {Errors}",
                    domainEvent.Email,
                    string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "[Phase 6A.53] ✅ Verification email sent successfully to {Email}",
                    domainEvent.Email);
            }
        }
        catch (Exception ex)
        {
            // FAIL-SILENT: Log but don't throw (ARCHITECT-REQUIRED)
            _logger.LogError(ex,
                "[Phase 6A.53] Error handling MemberVerificationRequestedEvent for User {UserId}",
                domainEvent.UserId);
            // Do NOT re-throw - prevents transaction rollback
        }
    }
}
