using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.81 Part 3: Sends email when Preliminary registration created.
/// Email contains payment link and 24h expiration notice.
///
/// Fail-silent: Email failures don't block registration transaction.
/// Architect approval: Matches Phase 6A.83 pattern, email is secondary notification.
///
/// User decision: Immediate email sending (not delayed).
/// Validation: Checks PaymentStatus before sending to prevent race condition.
/// </summary>
public class RegistrationPendingPaymentEventHandler
    : INotificationHandler<DomainEventNotification<RegistrationPendingPaymentEvent>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailService _emailService;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<RegistrationPendingPaymentEventHandler> _logger;

    public RegistrationPendingPaymentEventHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IEmailService emailService,
        IStripePaymentService stripePaymentService,
        ILogger<RegistrationPendingPaymentEventHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _emailService = emailService;
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

            // Prepare email parameters
            var parameters = new Dictionary<string, object>
            {
                { "UserName", domainEvent.ContactName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventLocation", @event.Location?.ToString() ?? "TBD" },
                { "AttendeeCount", domainEvent.AttendeeCount },
                { "TotalAmount", $"${domainEvent.TotalAmount:F2}" },
                { "Currency", domainEvent.Currency.ToUpper() },
                { "PaymentLink", checkoutUrl ?? string.Empty },
                { "ExpiresAt", domainEvent.CheckoutExpiresAt.ToString("MMMM dd, yyyy h:mm tt UTC") },
                { "HoursRemaining", hoursRemaining },
                { "RegistrationId", domainEvent.RegistrationId.ToString() },
                { "SupportEmail", "support@lankaconnect.com" },
                { "Year", DateTime.UtcNow.Year }
            };

            _logger.LogDebug(
                "[Phase 6A.81-Part3] [Email-4] Email parameters prepared - " +
                "ParameterCount={Count}",
                parameters.Count);

            // Send email
            await _emailService.SendTemplatedEmailAsync(
                EmailTemplateNames.PreliminaryRegistrationPayment,
                domainEvent.ContactEmail,
                parameters,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "[Phase 6A.81-Part3] [Email-COMPLETE] Preliminary registration email sent successfully - " +
                "RegistrationId={RegistrationId}, Email={Email}, ExpiresIn={HoursRemaining}h, Duration={ElapsedMs}ms",
                domainEvent.RegistrationId, domainEvent.ContactEmail, hoursRemaining, stopwatch.ElapsedMilliseconds);
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
