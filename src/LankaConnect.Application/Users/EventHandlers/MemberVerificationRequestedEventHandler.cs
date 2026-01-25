using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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

        using (LogContext.PushProperty("Operation", "MemberVerificationRequested"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        using (LogContext.PushProperty("Email", domainEvent.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "MemberVerificationRequested START: UserId={UserId}, Email={Email}",
                domainEvent.UserId, domainEvent.Email);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Generate verification URL
                var verificationUrl = _urlsService.GetEmailVerificationUrl(domainEvent.VerificationToken);

                // Phase 6A.53 Fix: Build user name from FirstName and LastName
                var userName = BuildUserName(domainEvent.FirstName, domainEvent.LastName);

                var parameters = new Dictionary<string, object>
                {
                    { "Email", domainEvent.Email },
                    { "VerificationUrl", verificationUrl },
                    { "TokenExpiry", "24 hours" },  // Phase 6A.83: Fix parameter name to match template
                    { "UserName", userName }
                };

                _logger.LogInformation(
                    "MemberVerificationRequested: Sending verification email - Email={Email}, UserName={UserName}",
                    domainEvent.Email, userName);

                var result = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.MemberEmailVerification,
                    domainEvent.Email,
                    parameters,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "MemberVerificationRequested FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.Email, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "MemberVerificationRequested COMPLETE: Email sent successfully - Email={Email}, Duration={ElapsedMs}ms",
                        domainEvent.Email, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "MemberVerificationRequested CANCELED: Operation was canceled - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.Email, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // FAIL-SILENT: Log but don't throw (ARCHITECT-REQUIRED)
                _logger.LogError(ex,
                    "MemberVerificationRequested FAILED: Exception occurred - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.Email, stopwatch.ElapsedMilliseconds);
                // Do NOT re-throw - prevents transaction rollback
            }
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
