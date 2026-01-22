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

            // Phase 6A.53 Fix: Build user name from FirstName and LastName
            var userName = BuildUserName(domainEvent.FirstName, domainEvent.LastName);

            var parameters = new Dictionary<string, object>
            {
                { "Email", domainEvent.Email },
                { "VerificationUrl", verificationUrl },
                { "ExpirationHours", 24 },
                { "UserName", userName }
            };

            _logger.LogInformation(
                "[Phase 6A.53] Sending member-email-verification to {Email} ({UserName}), VerificationUrl: {VerificationUrl}",
                domainEvent.Email, userName, verificationUrl);

            var result = await _emailService.SendTemplatedEmailAsync(
                "template-membership-email-verification",
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

    /// <summary>
    /// Phase 6A.53 Fix: Builds user name from first and last name
    /// Falls back to "Friend" if both names are empty
    /// </summary>
    private static string BuildUserName(string firstName, string lastName)
    {
        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
            return $"{first} {last}";

        if (!string.IsNullOrEmpty(first))
            return first;

        if (!string.IsNullOrEmpty(last))
            return last;

        return "Friend";
    }
}
