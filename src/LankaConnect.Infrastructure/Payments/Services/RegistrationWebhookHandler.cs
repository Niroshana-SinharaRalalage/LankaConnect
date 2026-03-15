using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 0: Handles Stripe webhook events for registration payments.
/// Extracted from PaymentsController for separation of concerns.
/// </summary>
public class RegistrationWebhookHandler : IRegistrationWebhookHandler
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegistrationWebhookHandler> _logger;

    public RegistrationWebhookHandler(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegistrationWebhookHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentStatus,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        // Extract metadata (regular registration payment)
        if (!metadata.TryGetValue("registration_id", out var registrationIdStr) ||
            !Guid.TryParse(registrationIdStr, out var registrationId))
        {
            _logger.LogWarning(
                "[Phase 6A.52] [Webhook-ERROR] Missing registration_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);
            return;
        }

        if (!metadata.TryGetValue("event_id", out var eventIdStr) ||
            !Guid.TryParse(eventIdStr, out var eventId))
        {
            _logger.LogWarning(
                "[Phase 6A.52] [Webhook-ERROR] Missing event_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-2] Metadata extracted - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}",
            correlationId, eventId, registrationId);

        // Phase 6A.52: Log before loading registration
        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-3] Loading registration - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
            correlationId, registrationId);

        // Phase 6A.49 FIX: Load Registration DIRECTLY with tracking enabled
        var registration = await _registrationRepository.GetByIdAsync(registrationId);
        if (registration == null)
        {
            _logger.LogError(
                "[Phase 6A.52] [Webhook-ERROR] Registration not found - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, SessionId: {SessionId}",
                correlationId, registrationId, sessionId);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-4] Registration loaded - CorrelationId: {CorrelationId}, PaymentStatus: {PaymentStatus}, CurrentStripePaymentIntentId: {StripePaymentIntentId}",
            correlationId, registration.PaymentStatus, registration.StripePaymentIntentId);

        // Phase 6A.81: Log registration state BEFORE payment completion
        _logger.LogInformation(
            "[Phase 6A.81] [Webhook-State] Before CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, CurrentPaymentStatus: {PaymentStatus}, CheckoutExpiresAt: {ExpiresAt}",
            correlationId, registrationId, registration.Status, registration.PaymentStatus, registration.CheckoutSessionExpiresAt?.ToString("o") ?? "null");

        // Verify registration belongs to the expected event (security check)
        if (registration.EventId != eventId)
        {
            _logger.LogError(
                "[Phase 6A.52] [Webhook-ERROR] Event mismatch - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, ActualEventId: {ActualEventId}, ExpectedEventId: {ExpectedEventId}",
                correlationId, registrationId, registration.EventId, eventId);
            return;
        }

        // Phase 6A.52: Log domain events BEFORE CompletePayment (with correlation ID)
        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-5] Before CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}",
            correlationId, registrationId, registration.DomainEvents.Count);

        // Complete payment on registration domain entity
        var completeResult = registration.CompletePayment(paymentIntentId);

        if (completeResult.IsFailure)
        {
            _logger.LogError(
                "[Phase 6A.52] [Webhook-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, Error: {Error}",
                correlationId, registrationId, completeResult.Error);
            return;
        }

        // Phase 6A.81: Log registration state AFTER payment completion (Preliminary -> Confirmed)
        _logger.LogInformation(
            "[Phase 6A.81] [Webhook-State] After CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewStatus: {Status}, NewPaymentStatus: {PaymentStatus}, Transition: Preliminary→Confirmed",
            correlationId, registrationId, registration.Status, registration.PaymentStatus);

        // Phase 6A.52: Log domain events AFTER CompletePayment
        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-6] After CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}, EventTypes: [{EventTypes}]",
            correlationId, registrationId, registration.DomainEvents.Count, string.Join(", ", registration.DomainEvents.Select(e => e.GetType().Name)));

        // Phase 6A.51 FIX: Restore Update() call (critical for domain event dispatch)
        _registrationRepository.Update(registration);

        // Phase 6A.52: Log BEFORE CommitAsync
        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-7] Before CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents: {Count}",
            correlationId, registrationId, registration.DomainEvents.Count);

        // Save changes and dispatch domain events
        await _unitOfWork.CommitAsync();

        // Phase 6A.52: Log AFTER CommitAsync
        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-8] After CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents: {Count} (should be cleared)",
            correlationId, registrationId, registration.DomainEvents.Count);

        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-SUCCESS] Payment completed successfully - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, PaymentIntentId: {PaymentIntentId}",
            correlationId, eventId, registrationId, paymentIntentId);

        // C2 Guard: Handle bundled donation in ISOLATED try-catch AFTER registration commits.
        // If donation fails, registration is already saved — never lose registration.
        if (metadata.TryGetValue("donation_id", out var donationIdStr) &&
            Guid.TryParse(donationIdStr, out var donationId))
        {
            try
            {
                _logger.LogInformation(
                    "[Donation] [Webhook-Bundled-1] Processing bundled donation - CorrelationId: {CorrelationId}, DonationId: {DonationId}, RegistrationId: {RegistrationId}",
                    correlationId, donationId, registrationId);

                var donation = await _donationRepository.GetByDonationIdAsync(donationId);
                if (donation == null)
                {
                    _logger.LogWarning(
                        "[Donation] [Webhook-Bundled-WARN] Bundled donation not found - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                        correlationId, donationId);
                }
                else
                {
                    var donationCompleteResult = donation.CompletePayment(paymentIntentId);
                    if (donationCompleteResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "[Donation] [Webhook-Bundled-WARN] Donation CompletePayment failed - CorrelationId: {CorrelationId}, DonationId: {DonationId}, Error: {Error}",
                            correlationId, donationId, donationCompleteResult.Error);
                    }
                    else
                    {
                        // Phase 6A.X FIX: Detach all non-Donation entities before second CommitAsync.
                        await _unitOfWork.ClearChangeTrackerExceptAsync<Domain.Events.Donation>();

                        _donationRepository.Update(donation);
                        await _unitOfWork.CommitAsync();

                        _logger.LogInformation(
                            "[Donation] [Webhook-Bundled-SUCCESS] Bundled donation completed - CorrelationId: {CorrelationId}, DonationId: {DonationId}, PaymentIntentId: {PaymentIntentId}",
                            correlationId, donationId, paymentIntentId);
                    }
                }
            }
            catch (Exception donationEx)
            {
                // C2 Guard: Never let donation failure affect registration.
                // Log error but return 200 OK to Stripe.
                _logger.LogError(donationEx,
                    "[Donation] [Webhook-Bundled-ERROR] Failed to process bundled donation (registration already committed) - CorrelationId: {CorrelationId}, DonationId: {DonationId}, ExceptionType: {ExceptionType}, Message: {ErrorMessage}, InnerException: {InnerMessage}",
                    correlationId, donationId, donationEx.GetType().FullName, donationEx.Message, donationEx.InnerException?.Message ?? "None");
            }
        }
    }

    /// <inheritdoc />
    public async Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        // Extract metadata
        if (!metadata.TryGetValue("registration_id", out var registrationIdStr) ||
            !Guid.TryParse(registrationIdStr, out var registrationId))
        {
            _logger.LogWarning(
                "[Phase 6A.81] [Webhook-Expired-WARN] Missing registration_id in metadata - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);
            return;
        }

        if (!metadata.TryGetValue("event_id", out var eventIdStr) ||
            !Guid.TryParse(eventIdStr, out var eventId))
        {
            _logger.LogWarning(
                "[Phase 6A.81] [Webhook-Expired-WARN] Missing event_id in metadata - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.81] [Webhook-Expired-2] Metadata extracted - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}",
            correlationId, eventId, registrationId);

        // Load registration
        var registration = await _registrationRepository.GetByIdAsync(registrationId);
        if (registration == null)
        {
            _logger.LogWarning(
                "[Phase 6A.81] [Webhook-Expired-WARN] Registration not found (might already be processed) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                correlationId, registrationId);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.81] [Webhook-Expired-3] Registration loaded - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, CurrentPaymentStatus: {PaymentStatus}",
            correlationId, registrationId, registration.Status, registration.PaymentStatus);

        // Verify registration belongs to the expected event (security check)
        if (registration.EventId != eventId)
        {
            _logger.LogError(
                "[Phase 6A.81] [Webhook-Expired-ERROR] Event mismatch - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, ActualEventId: {ActualEventId}, ExpectedEventId: {ExpectedEventId}",
                correlationId, registrationId, registration.EventId, eventId);
            return;
        }

        // Mark as abandoned (Preliminary -> Abandoned)
        var abandonResult = registration.MarkAbandoned();

        if (abandonResult.IsFailure)
        {
            // This is expected if registration was already completed or abandoned
            _logger.LogWarning(
                "[Phase 6A.81] [Webhook-Expired-INFO] MarkAbandoned failed (expected if already processed) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, Error: {Error}",
                correlationId, registrationId, registration.Status, abandonResult.Error);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.81] [Webhook-Expired-4] After MarkAbandoned - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewStatus: {Status}, NewPaymentStatus: {PaymentStatus}, Transition: Preliminary→Abandoned",
            correlationId, registrationId, registration.Status, registration.PaymentStatus);

        // Save changes
        _registrationRepository.Update(registration);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "[Phase 6A.81] [Webhook-Expired-SUCCESS] Checkout session expired, registration marked as Abandoned - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, AbandonedAt: {AbandonedAt}",
            correlationId, eventId, registrationId, registration.AbandonedAt?.ToString("o") ?? "null");

        // C4 Guard: If this was a combined checkout, also abandon the bundled donation.
        // Separate try-catch so registration abandonment is preserved even if donation fails.
        if (metadata.TryGetValue("donation_id", out var donationIdStr) &&
            Guid.TryParse(donationIdStr, out var donationId))
        {
            try
            {
                _logger.LogInformation(
                    "[Donation] [Webhook-Expired-Donation-1] Abandoning bundled donation - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                    correlationId, donationId);

                var donation = await _donationRepository.GetByDonationIdAsync(donationId);
                if (donation != null && donation.Status == Domain.Events.Enums.DonationStatus.Pending)
                {
                    donation.MarkAsAbandoned();
                    _donationRepository.Update(donation);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "[Donation] [Webhook-Expired-Donation-SUCCESS] Bundled donation abandoned - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                        correlationId, donationId);
                }
                else
                {
                    _logger.LogWarning(
                        "[Donation] [Webhook-Expired-Donation-WARN] Donation not found or not Pending - CorrelationId: {CorrelationId}, DonationId: {DonationId}, Status: {Status}",
                        correlationId, donationId, donation?.Status.ToString() ?? "NULL");
                }
            }
            catch (Exception donationEx)
            {
                // C4 Guard: Don't fail the overall expiry handling if donation cleanup fails
                _logger.LogError(donationEx,
                    "[Donation] [Webhook-Expired-Donation-ERROR] Failed to abandon bundled donation - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                    correlationId, donationId);
            }
        }
    }

    /// <inheritdoc />
    public async Task HandleChargeRefundedAsync(
        string chargeId,
        string? paymentIntentId,
        string refundId,
        long amountRefunded,
        Dictionary<string, string>? refundMetadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 6A.91] [Webhook-Refund-2] Refund details received - CorrelationId: {CorrelationId}, RefundId: {RefundId}, AmountRefunded: {Amount}",
            correlationId, refundId, amountRefunded);

        // Phase 6A.X FIX: Try to find registration via metadata first, then fallback to PaymentIntentId
        Registration? registration = null;

        // Method 1: Extract registration_id from refund metadata (we store it when creating refund)
        if (refundMetadata != null &&
            refundMetadata.TryGetValue("registration_id", out var registrationIdStr) &&
            Guid.TryParse(registrationIdStr, out var registrationId))
        {
            _logger.LogInformation(
                "[Phase 6A.91] [Webhook-Refund-3a] Metadata lookup - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                correlationId, registrationId);

            registration = await _registrationRepository.GetByIdAsync(registrationId);
        }

        // Method 2 (FALLBACK): Use PaymentIntentId from charge if metadata is missing
        if (registration == null && !string.IsNullOrEmpty(paymentIntentId))
        {
            _logger.LogInformation(
                "[Phase 6A.91] [Webhook-Refund-3b] Fallback to PaymentIntentId lookup - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, paymentIntentId);

            registration = await _registrationRepository.GetByPaymentIntentIdAsync(paymentIntentId);
        }

        // If still not found, we cannot process the refund
        if (registration == null)
        {
            _logger.LogWarning(
                "[Phase 6A.91] [Webhook-Refund-WARN] Registration not found by metadata or PaymentIntentId - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, chargeId, paymentIntentId);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.91] [Webhook-Refund-4] Registration loaded - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, CurrentPaymentStatus: {PaymentStatus}",
            correlationId, registration.Id, registration.Status, registration.PaymentStatus);

        // Complete refund (RefundRequested -> Refunded)
        var completeRefundResult = registration.CompleteRefund(refundId);

        if (completeRefundResult.IsFailure)
        {
            // This may be expected if refund was already processed (idempotency)
            _logger.LogWarning(
                "[Phase 6A.91] [Webhook-Refund-INFO] CompleteRefund failed (may be already processed) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, Error: {Error}",
                correlationId, registration.Id, registration.Status, completeRefundResult.Error);
            return;
        }

        _logger.LogInformation(
            "[Phase 6A.91] [Webhook-Refund-5] After CompleteRefund - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewStatus: {Status}, NewPaymentStatus: {PaymentStatus}, StripeRefundId: {RefundId}, Transition: RefundRequested→Refunded",
            correlationId, registration.Id, registration.Status, registration.PaymentStatus, refundId);

        // Phase 6A.92 FIX: Log domain events to diagnose email dispatch issue
        _logger.LogInformation(
            "[Phase 6A.92] [Webhook-Refund-6] Domain events after CompleteRefund - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}, EventTypes: [{EventTypes}]",
            correlationId, registration.Id, registration.DomainEvents.Count, string.Join(", ", registration.DomainEvents.Select(e => e.GetType().Name)));

        // Save changes and dispatch RefundCompletedEvent
        _registrationRepository.Update(registration);

        _logger.LogInformation(
            "[Phase 6A.92] [Webhook-Refund-7] Before CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}",
            correlationId, registration.Id, registration.DomainEvents.Count);

        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "[Phase 6A.92] [Webhook-Refund-8] After CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count} (should be cleared after dispatch)",
            correlationId, registration.Id, registration.DomainEvents.Count);

        _logger.LogInformation(
            "[Phase 6A.91] [Webhook-Refund-SUCCESS] Refund completed successfully - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, StripeRefundId: {RefundId}, RefundCompletedAt: {RefundCompletedAt}",
            correlationId, registration.Id, refundId, registration.RefundCompletedAt?.ToString("o") ?? "null");
    }
}
