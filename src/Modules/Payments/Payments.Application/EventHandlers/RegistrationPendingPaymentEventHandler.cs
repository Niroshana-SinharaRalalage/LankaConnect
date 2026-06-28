using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Payments.Application.EventHandlers;

/// <summary>
/// Phase 6A.100: Sends email when Preliminary registration created.
/// Migrated from IEmailService to ITypedEmailService with PreliminaryRegistrationPaymentEmailParams.
///
/// Email contains payment link and 24h expiration notice.
/// Fail-silent: Email failures don't block registration transaction.
/// User decision: Immediate email sending (not delayed).
/// Validation: Checks PaymentStatus before sending to prevent race condition.
/// </summary>
public class RegistrationPendingPaymentEventHandler
    : INotificationHandler<DomainEventNotification<RegistrationPendingPaymentEvent>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<RegistrationPendingPaymentEventHandler> _logger;

    public RegistrationPendingPaymentEventHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ITypedEmailService typedEmailService,
        IStripePaymentService stripePaymentService,
        ILogger<RegistrationPendingPaymentEventHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _typedEmailService = typedEmailService;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<RegistrationPendingPaymentEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using var _1 = LogContext.PushProperty("CorrelationId", Guid.NewGuid());
        using var _2 = LogContext.PushProperty("Phase", "6A.81-Part3");
        using var _3 = LogContext.PushProperty("Operation", "SendPreliminaryRegistrationEmail");
        using var _4 = LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId);
        using var _5 = LogContext.PushProperty("EventId", domainEvent.EventId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "[Phase 6A.81-Part3] [Email-START] Sending preliminary registration email - " +
                "RegistrationId={RegistrationId}, EventId={EventId}, Email={Email}",
                domainEvent.RegistrationId, domainEvent.EventId, domainEvent.ContactEmail);

            // CRITICAL: Check if payment already completed (race condition protection)
            // User concern: "If user completed payment later, payment link should be invalid"
            var registration = await _registrationRepository.GetByIdAsync(
                domainEvent.RegistrationId,
                cancellationToken);

            if (registration == null)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.81-Part3] [Email-SKIPPED] Registration not found - " +
                    "RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                    domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
                return;  // Fail silent
            }

            if (registration.PaymentStatus == PaymentStatus.Completed)
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "[Phase 6A.81-Part3] [Email-SKIPPED] Payment already completed, skipping pending email - " +
                    "RegistrationId={RegistrationId}, PaymentStatus={PaymentStatus}, Duration={ElapsedMs}ms",
                    domainEvent.RegistrationId, registration.PaymentStatus, stopwatch.ElapsedMilliseconds);
                return;  // Don't send "pending payment" email if payment already completed
            }

            // Get event details
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                stopwatch.Stop();
                _logger.LogError(
                    "[Phase 6A.81-Part3] [Email-FAILED] Event not found - " +
                    "EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                return;  // Fail silent
            }

            _logger.LogDebug(
                "[Phase 6A.81-Part3] [Email-1] Event details retrieved - " +
                "EventId={EventId}, Title={Title}, StartDate={StartDate}",
                @event.Id, @event.Title.Value, @event.StartDate);

            // Get checkout URL from Stripe
            var checkoutUrlResult = await _stripePaymentService
                .GetCheckoutSessionUrlAsync(
                    domainEvent.StripeCheckoutSessionId,
                    cancellationToken);

            if (checkoutUrlResult.IsFailure)
            {
                stopwatch.Stop();
                _logger.LogError(
                    "[Phase 6A.81-Part3] [Email-FAILED] Failed to retrieve checkout URL - " +
                    "SessionId={SessionId}, Error={Error}, Duration={ElapsedMs}ms",
                    domainEvent.StripeCheckoutSessionId, checkoutUrlResult.Error, stopwatch.ElapsedMilliseconds);
                return;  // Fail silent
            }

            var checkoutUrl = checkoutUrlResult.Value;

            _logger.LogDebug(
                "[Phase 6A.81-Part3] [Email-2] Stripe checkout URL retrieved - " +
                "SessionId={SessionId}, UrlLength={UrlLength}",
                domainEvent.StripeCheckoutSessionId, checkoutUrl?.Length ?? 0);

            // Calculate expiration time remaining
            var now = DateTime.UtcNow;
            var expiresIn = domainEvent.CheckoutExpiresAt - now;
            var hoursRemaining = Math.Max(0, (int)expiresIn.TotalHours);

            _logger.LogDebug(
                "[Phase 6A.81-Part3] [Email-3] Expiration calculated - " +
                "ExpiresAt={ExpiresAt}, HoursRemaining={HoursRemaining}",
                domainEvent.CheckoutExpiresAt.ToString("o"), hoursRemaining);

            // Phase 6A.100: Use typed email params
            var emailParams = PreliminaryRegistrationPaymentEmailParams.Create(
                recipientEmail: domainEvent.ContactEmail,
                userName: domainEvent.ContactName,
                eventId: @event.Id,
                eventTitle: @event.Title.Value,
                eventStartDate: @event.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: pending payment can't fire on TBD today
                timeZoneId: @event.TimeZoneId,
                eventLocation: @event.Location?.ToString() ?? "TBD",
                registrationId: domainEvent.RegistrationId,
                attendeeCount: domainEvent.AttendeeCount,
                totalAmount: domainEvent.TotalAmount,
                currency: domainEvent.Currency.ToUpper(),
                paymentLink: checkoutUrl ?? string.Empty,
                expiresAt: domainEvent.CheckoutExpiresAt);

            // Phase 7C.2b: emit decomposed location keys for the template rewrite.
            emailParams.WithLocationDetails(@event.ProjectEmailLocation());

            _logger.LogDebug(
                "[Phase 6A.100] [Email-4] Email parameters prepared using typed params");

            // Send email using typed service
            var result = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

            stopwatch.Stop();

            if (result.Success)
            {
                _logger.LogInformation(
                    "[Phase 6A.100] [Email-COMPLETE] Preliminary registration email sent successfully - " +
                    "RegistrationId={RegistrationId}, Email={Email}, ExpiresIn={HoursRemaining}h, Duration={ElapsedMs}ms",
                    domainEvent.RegistrationId, domainEvent.ContactEmail, hoursRemaining, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.100] [Email-FAILED] Preliminary registration email failed - " +
                    "RegistrationId={RegistrationId}, Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                    domainEvent.RegistrationId, domainEvent.ContactEmail, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Fail silent: Log error but don't throw (email failure shouldn't block registration)
            _logger.LogError(ex,
                "[Phase 6A.81-Part3] [Email-EXCEPTION] Failed to send preliminary registration email - " +
                "RegistrationId={RegistrationId}, EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                domainEvent.RegistrationId, domainEvent.EventId, domainEvent.ContactEmail,
                stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }
}
