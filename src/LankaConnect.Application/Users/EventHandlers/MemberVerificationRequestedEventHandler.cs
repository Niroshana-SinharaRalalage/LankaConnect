using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users.DomainEvents;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.EventHandlers;

/// <summary>
/// Handles MemberVerificationRequestedEvent to send email verification link.
/// Phase 6A.53: Member Email Verification System
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// Uses fail-silent pattern to prevent transaction rollback.
/// </summary>
public class MemberVerificationRequestedEventHandler
    : INotificationHandler<DomainEventNotification<MemberVerificationRequestedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly ILogger<MemberVerificationRequestedEventHandler> _logger;
    private readonly IApplicationUrlsService _urlsService;

    private const string HandlerName = nameof(MemberVerificationRequestedEventHandler);  // Phase 6A.87: For feature flag lookup

    public MemberVerificationRequestedEventHandler(
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        ILogger<MemberVerificationRequestedEventHandler> logger,
        IApplicationUrlsService urlsService)
    {
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
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

                // Phase 6A.87: Use typed email parameters for compile-time safety
                var emailParams = EmailVerificationEmailParams.Create(
                    userId: domainEvent.UserId,
                    userName: userName,
                    email: domainEvent.Email,
                    verificationUrl: verificationUrl,
                    expirationHours: "24"
                );

                _logger.LogInformation(
                    "MemberVerificationRequested: Sending verification email - Email={Email}, UserName={UserName}",
                    domainEvent.Email, userName);

                // Phase 6A.87: Send via typed email service (feature flags handled internally)
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    HandlerName,
                    cancellationToken);

                stopwatch.Stop();

                if (!typedResult.Success)
                {
                    _logger.LogError(
                        "MemberVerificationRequested FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.Email, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "MemberVerificationRequested COMPLETE: Email sent successfully - Email={Email}, UsedTyped={UsedTyped}, Duration={ElapsedMs}ms",
                        domainEvent.Email, typedResult.UsedTypedParameters, stopwatch.ElapsedMilliseconds);
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
