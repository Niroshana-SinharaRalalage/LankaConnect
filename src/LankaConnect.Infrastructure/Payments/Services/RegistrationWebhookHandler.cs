using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 0: Handles Stripe webhook events for registration payments.
/// Extracted from PaymentsController for separation of concerns.
/// Phase 6A.137D: Extended to handle bundled add-on purchases.
/// Phase 6A.137E: Extended to handle bundled collections and sponsors.
/// </summary>
public class RegistrationWebhookHandler : IRegistrationWebhookHandler
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISponsorRepository _sponsorRepository;
    // Phase 8 S8.2.C: dependencies for hold→reservation conversion on payment completion
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatReservationRepository _seatReservationRepository;
    private readonly ISeatHoldMetrics _seatHoldMetrics;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegistrationWebhookHandler> _logger;

    public RegistrationWebhookHandler(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        ICollectionRepository collectionRepository,
        ISponsorRepository sponsorRepository,
        ISeatHoldRepository seatHoldRepository,
        ISeatReservationRepository seatReservationRepository,
        ISeatHoldMetrics seatHoldMetrics,
        IUnitOfWork unitOfWork,
        ILogger<RegistrationWebhookHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _collectionRepository = collectionRepository;
        _sponsorRepository = sponsorRepository;
        _seatHoldRepository = seatHoldRepository;
        _seatReservationRepository = seatReservationRepository;
        _seatHoldMetrics = seatHoldMetrics;
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

        // Phase 8X.4b — Defence-in-depth: ExternalPaid events should never enter the
        // Stripe pipeline (no internal Registration → no Stripe Checkout Session created).
        // A webhook arriving for an ExternalPaid event is either (a) attacker-crafted
        // payload, or (b) state corruption (registration row exists for ExternalPaid event).
        // Either way: log warning + return 200 (no exception) so Stripe doesn't retry.
        try
        {
            var defensiveEvent = await _eventRepository.GetByIdAsync(eventId, ct);
            if (defensiveEvent != null && defensiveEvent.PaymentMode == EventPaymentMode.ExternalPaid)
            {
                _logger.LogWarning(
                    "[Phase 8X.4b] Stripe webhook received for ExternalPaid event — ignoring to prevent Stripe pipeline invocation. " +
                    "CorrelationId: {CorrelationId}, EventId: {EventId}, SessionId: {SessionId}, RegistrationId: {RegistrationId}",
                    correlationId, eventId, sessionId, registrationId);
                return;
            }
        }
        catch (Exception ex)
        {
            // Lookup failure must not block the legitimate happy path. Log and proceed —
            // the existing webhook flow will surface its own errors if the event truly
            // doesn't exist or registration mismatches.
            _logger.LogError(ex,
                "[Phase 8X.4b] Defensive event lookup faulted — proceeding with webhook flow. " +
                "CorrelationId: {CorrelationId}, EventId: {EventId}",
                correlationId, eventId);
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

        // Phase 6A.137F-Fix4: Complete ALL bundled items BEFORE CommitAsync so that
        // PaymentCompletedEvent (dispatched inline by CommitAsync) sees them as Completed.
        // Previous approach completed bundled items AFTER CommitAsync, causing a race condition
        // where email handler and payment success queries found add-ons still in Pending status.

        // C2 Guard: Handle bundled donation in ISOLATED try-catch.
        // If donation fails, registration payment still completes.
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
                        _donationRepository.Update(donation);

                        _logger.LogInformation(
                            "[Donation] [Webhook-Bundled-SUCCESS] Bundled donation marked completed (will persist with single CommitAsync) - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                            correlationId, donationId);
                    }
                }
            }
            catch (Exception donationEx)
            {
                // C2 Guard: Never let donation failure affect registration.
                _logger.LogError(donationEx,
                    "[Donation] [Webhook-Bundled-ERROR] Failed to process bundled donation - CorrelationId: {CorrelationId}, DonationId: {DonationId}, ExceptionType: {ExceptionType}, Message: {ErrorMessage}, InnerException: {InnerMessage}",
                    correlationId, donationId, donationEx.GetType().FullName, donationEx.Message, donationEx.InnerException?.Message ?? "None");
            }
        }

        // Phase 6A.137D: C2 Guard: Handle bundled add-on purchases in ISOLATED try-catch.
        if (metadata.TryGetValue("addon_purchase_ids", out var addOnPurchaseIdsStr) &&
            !string.IsNullOrWhiteSpace(addOnPurchaseIdsStr))
        {
            try
            {
                var addOnIds = addOnPurchaseIdsStr.Split(',')
                    .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : (Guid?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                _logger.LogInformation(
                    "[AddOn] [Webhook-Bundled-1] Processing {Count} bundled add-on purchase(s) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    addOnIds.Count, correlationId, registrationId);

                var completedCount = 0;
                foreach (var addOnPurchaseId in addOnIds)
                {
                    try
                    {
                        var purchase = await _addOnPurchaseRepository.GetByIdAsync(addOnPurchaseId);
                        if (purchase == null)
                        {
                            _logger.LogWarning(
                                "[AddOn] [Webhook-Bundled-WARN] Add-on purchase not found - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}",
                                correlationId, addOnPurchaseId);
                            continue;
                        }

                        if (purchase.Status != AddOnPurchaseStatus.Pending)
                        {
                            _logger.LogWarning(
                                "[AddOn] [Webhook-Bundled-WARN] Add-on purchase not Pending (idempotent skip) - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Status: {Status}",
                                correlationId, addOnPurchaseId, purchase.Status);
                            continue;
                        }

                        var completeAddOnResult = purchase.CompletePayment(paymentIntentId);
                        if (completeAddOnResult.IsFailure)
                        {
                            _logger.LogWarning(
                                "[AddOn] [Webhook-Bundled-WARN] Add-on CompletePayment failed - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Error: {Error}",
                                correlationId, addOnPurchaseId, completeAddOnResult.Error);
                            continue;
                        }

                        _addOnPurchaseRepository.Update(purchase);
                        completedCount++;

                        _logger.LogInformation(
                            "[AddOn] [Webhook-Bundled-2] Add-on payment completed - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}",
                            correlationId, addOnPurchaseId);
                    }
                    catch (Exception addOnItemEx)
                    {
                        _logger.LogError(addOnItemEx,
                            "[AddOn] [Webhook-Bundled-ERROR] Failed to process individual add-on (continuing) - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}",
                            correlationId, addOnPurchaseId);
                    }
                }

                _logger.LogInformation(
                    "[AddOn] [Webhook-Bundled-SUCCESS] {CompletedCount} bundled add-on purchase(s) marked completed (will persist with single CommitAsync) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    completedCount, correlationId, registrationId);
            }
            catch (Exception addOnEx)
            {
                // C2 Guard: Never let add-on failure affect registration.
                _logger.LogError(addOnEx,
                    "[AddOn] [Webhook-Bundled-ERROR] Failed to process bundled add-ons - CorrelationId: {CorrelationId}, AddOnPurchaseIds: {Ids}",
                    correlationId, addOnPurchaseIdsStr);
            }
        }

        // Phase 6A.137E: C2 Guard: Handle bundled collection in ISOLATED try-catch.
        if (metadata.TryGetValue("collection_id", out var collectionIdStr) &&
            Guid.TryParse(collectionIdStr, out var collectionId))
        {
            try
            {
                _logger.LogInformation(
                    "[Collection] [Webhook-Bundled-1] Processing bundled collection - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}",
                    correlationId, collectionId);

                var collection = await _collectionRepository.GetByIdAsync(collectionId);
                if (collection != null && collection.Status == CollectionStatus.Pending)
                {
                    var collectionCompleteResult = collection.CompletePayment(paymentIntentId);
                    if (collectionCompleteResult.IsSuccess)
                    {
                        _collectionRepository.Update(collection);

                        _logger.LogInformation(
                            "[Collection] [Webhook-Bundled-SUCCESS] Bundled collection marked completed (will persist with single CommitAsync) - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}",
                            correlationId, collectionId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Collection] [Webhook-Bundled-WARN] CompletePayment failed - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, Error: {Error}",
                            correlationId, collectionId, collectionCompleteResult.Error);
                    }
                }
            }
            catch (Exception collectionEx)
            {
                _logger.LogError(collectionEx,
                    "[Collection] [Webhook-Bundled-ERROR] Failed to process bundled collection - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}",
                    correlationId, collectionId);
            }
        }

        // Phase 6A.137E: C2 Guard: Handle bundled sponsor in ISOLATED try-catch.
        if (metadata.TryGetValue("sponsor_id", out var sponsorIdStr) &&
            Guid.TryParse(sponsorIdStr, out var sponsorId))
        {
            try
            {
                _logger.LogInformation(
                    "[Sponsor] [Webhook-Bundled-1] Processing bundled sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                    correlationId, sponsorId);

                var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
                if (sponsor != null && sponsor.Status == SponsorStatus.Pending)
                {
                    var sponsorCompleteResult = sponsor.CompletePayment(paymentIntentId);
                    if (sponsorCompleteResult.IsSuccess)
                    {
                        _sponsorRepository.Update(sponsor);

                        _logger.LogInformation(
                            "[Sponsor] [Webhook-Bundled-SUCCESS] Bundled sponsor marked completed (will persist with single CommitAsync) - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                            correlationId, sponsorId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Sponsor] [Webhook-Bundled-WARN] CompletePayment failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Error: {Error}",
                            correlationId, sponsorId, sponsorCompleteResult.Error);
                    }
                }
            }
            catch (Exception sponsorEx)
            {
                _logger.LogError(sponsorEx,
                    "[Sponsor] [Webhook-Bundled-ERROR] Failed to process bundled sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                    correlationId, sponsorId);
            }
        }

        // Phase 6A.52: Log domain events BEFORE CompletePayment (with correlation ID)
        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-5] Before CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}",
            correlationId, registrationId, registration.DomainEvents.Count);

        // Complete payment on registration domain entity
        // This adds PaymentCompletedEvent which triggers email handler inline during CommitAsync
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

        // ============================================================
        // Phase 8 S8.2.C: Convert pending seat assignments → reservations
        // ============================================================
        // Architect ADR-011: read PendingSeatAssignments, pre-flight check
        // for race-loss against the seat_reservations unique index, insert
        // SeatReservation rows for the all-clear case, confirm matching
        // SeatHolds, bind seat-ids to attendees via ConfirmSeatAssignments,
        // clear pending state. All-or-nothing on race-loss: registration
        // ends "confirmed-but-unseated"; ops handles via S8.4 audit script.
        //
        // R2 (rare TOCTOU between pre-flight and CommitAsync): the
        // postgres unique index on seat_reservations.seat_id throws 23505
        // at CommitAsync, the whole transaction including CompletePayment
        // rolls back, Stripe retries the webhook, and the retry's
        // pre-flight will detect the racing reservation and take the
        // confirmed-but-unseated path. Self-healing without bespoke retry.
        //
        // Outer try-catch: payment confirms regardless. Architect Q2/R4.
        // ============================================================
        if (registration.PendingSeatAssignments.Count > 0)
        {
            try
            {
                await ConvertPendingSeatAssignmentsAsync(registration, correlationId, ct);
            }
            catch (Exception seatEx)
            {
                // Defence-in-depth: any unexpected error in seat conversion is
                // logged + swallowed. Payment must confirm. Operator handles via
                // S8.4 audit (registrations with PaymentCompleted but no seat
                // labels) plus the new seat_conversion.race_lost metric.
                _logger.LogError(seatEx,
                    "[Phase 8 S8.2.C] [Webhook-SeatConversion-ERROR] Unexpected error converting pending seats — payment WILL still complete. CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    correlationId, registrationId);

                // Always clear the pending stash even on error so the next
                // event handler / retry doesn't re-attempt this conversion.
                try
                {
                    registration.ClearPendingSeatAssignments();
                }
                catch (Exception clearEx)
                {
                    _logger.LogWarning(clearEx,
                        "[Phase 8 S8.2.C] [Webhook-SeatConversion-WARN] Failed to ClearPendingSeatAssignments after error. CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                        correlationId, registrationId);
                }
            }
        }

        // Phase 6A.51 FIX: Restore Update() call (critical for domain event dispatch)
        _registrationRepository.Update(registration);

        // Phase 6A.52: Log BEFORE CommitAsync
        _logger.LogInformation(
            "[Phase 6A.137F-Fix4] [Webhook-7] Before SINGLE CommitAsync (registration + all bundled items) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents: {Count}",
            correlationId, registrationId, registration.DomainEvents.Count);

        // Phase 6A.137F-Fix4: SINGLE CommitAsync persists registration + all bundled items together.
        // PaymentCompletedEvent is dispatched INLINE by CommitAsync — email handler will now see
        // all bundled items as Completed because they were marked before this commit.
        await _unitOfWork.CommitAsync();

        // Phase 6A.52: Log AFTER CommitAsync
        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-8] After CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents: {Count} (should be cleared)",
            correlationId, registrationId, registration.DomainEvents.Count);

        _logger.LogInformation(
            "[Phase 6A.52] [Webhook-SUCCESS] Payment completed successfully - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, PaymentIntentId: {PaymentIntentId}",
            correlationId, eventId, registrationId, paymentIntentId);
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

        // ============================================================
        // Phase 8 S8.2.C: Release pending seat holds eagerly on checkout-expired
        // ============================================================
        // Symmetric counterpart to the checkout-completed conversion block:
        // when a buyer abandons, release their seat holds immediately so other
        // buyers can claim those seats without waiting for the 10-min TTL.
        // Best-effort — wrapped in try-catch so any failure here doesn't block
        // the abandonment commit. The cleanup background service is a backstop.
        // ============================================================
        if (registration.PendingSeatAssignments.Count > 0)
        {
            try
            {
                if (!string.IsNullOrEmpty(registration.PendingSeatSessionId))
                {
                    var holds = await _seatHoldRepository.GetActiveHoldsBySessionAsync(
                        registration.PendingSeatSessionId, ct);
                    var releasedCount = 0;
                    foreach (var hold in holds)
                    {
                        var releaseResult = hold.Release();
                        if (releaseResult.IsFailure)
                        {
                            _logger.LogInformation(
                                "[Phase 8 S8.2.C] [Webhook-Expired-HoldRelease] Skipping hold {HoldId} (status={Status}) - CorrelationId: {CorrelationId}",
                                hold.Id, hold.Status, correlationId);
                            continue;
                        }
                        _seatHoldRepository.Update(hold);
                        releasedCount++;
                    }
                    _logger.LogInformation(
                        "[Phase 8 S8.2.C] [Webhook-Expired-HoldRelease-SUCCESS] Released {ReleasedCount}/{HoldCount} pending holds early - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, SessionId: {SessionId}",
                        releasedCount, holds.Count, correlationId, registrationId, registration.PendingSeatSessionId);
                }
                registration.ClearPendingSeatAssignments();
            }
            catch (Exception holdEx)
            {
                _logger.LogWarning(holdEx,
                    "[Phase 8 S8.2.C] [Webhook-Expired-HoldRelease-WARN] Failed to release seat holds (non-fatal — cleanup service will handle). CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    correlationId, registrationId);
                // Even if hold release failed, clear the stash so we don't loop on retry
                try { registration.ClearPendingSeatAssignments(); }
                catch (Exception clearEx)
                {
                    _logger.LogWarning(clearEx,
                        "[Phase 8 S8.2.C] [Webhook-Expired-HoldRelease-WARN] Failed to ClearPendingSeatAssignments after error. CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                        correlationId, registrationId);
                }
            }
        }

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
                if (donation != null && donation.Status == LankaConnect.Products.LankaEvents.Domain.Enums.DonationStatus.Pending)
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

        // Phase 6A.137D: C4 Guard: Abandon bundled add-on purchases and restore stock.
        if (metadata.TryGetValue("addon_purchase_ids", out var addOnPurchaseIdsStr) &&
            !string.IsNullOrWhiteSpace(addOnPurchaseIdsStr))
        {
            try
            {
                var addOnIds = addOnPurchaseIdsStr.Split(',')
                    .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : (Guid?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                _logger.LogInformation(
                    "[AddOn] [Webhook-Expired-AddOn-1] Abandoning {Count} bundled add-on purchase(s) - CorrelationId: {CorrelationId}",
                    addOnIds.Count, correlationId);

                var abandonedCount = 0;
                foreach (var addOnPurchaseId in addOnIds)
                {
                    try
                    {
                        var purchase = await _addOnPurchaseRepository.GetByIdAsync(addOnPurchaseId);
                        if (purchase == null || purchase.Status != AddOnPurchaseStatus.Pending)
                            continue;

                        purchase.MarkAsAbandoned();

                        // Restore reserved stock
                        await _addOnDefinitionRepository.TryRestoreStockAsync(
                            purchase.AddOnDefinitionId, purchase.Quantity, ct);

                        _addOnPurchaseRepository.Update(purchase);
                        abandonedCount++;
                    }
                    catch (Exception addOnItemEx)
                    {
                        _logger.LogError(addOnItemEx,
                            "[AddOn] [Webhook-Expired-AddOn-ERROR] Failed to abandon individual add-on - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}",
                            correlationId, addOnPurchaseId);
                    }
                }

                if (abandonedCount > 0)
                {
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "[AddOn] [Webhook-Expired-AddOn-SUCCESS] {Count} bundled add-on purchase(s) abandoned, stock restored - CorrelationId: {CorrelationId}",
                        abandonedCount, correlationId);
                }
            }
            catch (Exception addOnEx)
            {
                _logger.LogError(addOnEx,
                    "[AddOn] [Webhook-Expired-AddOn-ERROR] Failed to abandon bundled add-ons - CorrelationId: {CorrelationId}, Ids: {Ids}",
                    correlationId, addOnPurchaseIdsStr);
            }
        }

        // Phase 6A.137E: C4 Guard: Abandon bundled collection.
        if (metadata.TryGetValue("collection_id", out var expCollectionIdStr) &&
            Guid.TryParse(expCollectionIdStr, out var expCollectionId))
        {
            try
            {
                var collection = await _collectionRepository.GetByIdAsync(expCollectionId);
                if (collection != null && collection.Status == CollectionStatus.Pending)
                {
                    collection.MarkAsAbandoned();
                    _collectionRepository.Update(collection);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "[Collection] [Webhook-Expired-SUCCESS] Bundled collection abandoned - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}",
                        correlationId, expCollectionId);
                }
            }
            catch (Exception collectionEx)
            {
                _logger.LogError(collectionEx,
                    "[Collection] [Webhook-Expired-ERROR] Failed to abandon bundled collection - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}",
                    correlationId, expCollectionId);
            }
        }

        // Phase 6A.137E: C4 Guard: Abandon bundled sponsor.
        if (metadata.TryGetValue("sponsor_id", out var expSponsorIdStr) &&
            Guid.TryParse(expSponsorIdStr, out var expSponsorId))
        {
            try
            {
                var sponsor = await _sponsorRepository.GetByIdAsync(expSponsorId);
                if (sponsor != null && sponsor.Status == SponsorStatus.Pending)
                {
                    sponsor.MarkAsAbandoned();
                    _sponsorRepository.Update(sponsor);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "[Sponsor] [Webhook-Expired-SUCCESS] Bundled sponsor abandoned - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                        correlationId, expSponsorId);
                }
            }
            catch (Exception sponsorEx)
            {
                _logger.LogError(sponsorEx,
                    "[Sponsor] [Webhook-Expired-ERROR] Failed to abandon bundled sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                    correlationId, expSponsorId);
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

        // Phase 6A.148.W5.D5: workflow-aware branch. When refund metadata says `refund_type=registration`
        // AND the registration is in Cancelled state (because the attendee cancelled separately through
        // CancelRsvpCommandHandler in the decoupled 6A.148 model, and the workflow refund's per-line
        // Stripe call is what fired this charge.refunded), the legacy `CompleteRefund` would refuse
        // (it requires Status=RefundRequested) — leaving the registration stuck in Cancelled forever
        // even though Stripe has refunded the money. The W5.D4 `CompleteRefundFromCancelled` is the
        // permissive counterpart that handles {RefundRequested, Cancelled}.
        //
        // Why we gate on metadata: pre-6A.148 refunds that hit this handler legitimately require
        // Status=RefundRequested (CancelRsvp + RegistrationRefundService inline flow). The W5.D5
        // branch only activates for refunds clearly originating from the workflow path so legacy
        // semantics are preserved.
        var isWorkflowRefund = refundMetadata != null
            && refundMetadata.TryGetValue("refund_type", out var refundType)
            && string.Equals(refundType, "registration", StringComparison.OrdinalIgnoreCase)
            && refundMetadata.ContainsKey("refund_request_id");

        Result completeRefundResult;
        if (isWorkflowRefund)
        {
            _logger.LogInformation(
                "[Phase 6A.148.W5.D5] [Webhook-Refund-Workflow] Workflow-owned ticket refund detected — routing to CompleteRefundFromCancelled - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, RefundRequestId: {RrId}",
                correlationId, registration.Id, registration.Status,
                refundMetadata!.TryGetValue("refund_request_id", out var rrId) ? rrId : "unknown");
            completeRefundResult = registration.CompleteRefundFromCancelled(refundId);
        }
        else
        {
            // Legacy path — preserved verbatim. Requires Status=RefundRequested.
            completeRefundResult = registration.CompleteRefund(refundId);
        }

        if (completeRefundResult.IsFailure)
        {
            // This may be expected if refund was already processed (idempotency)
            _logger.LogWarning(
                "[Phase 6A.91] [Webhook-Refund-INFO] CompleteRefund failed (may be already processed) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, IsWorkflow: {IsWorkflow}, Error: {Error}",
                correlationId, registration.Id, registration.Status, isWorkflowRefund, completeRefundResult.Error);
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

    /// <summary>
    /// Phase 8 S8.2.C — converts a registration's <c>PendingSeatAssignments</c> stash into
    /// permanent <c>SeatReservation</c> rows + bound <c>AttendeeDetails.SeatId</c> values.
    /// Called only when the buyer just transitioned Preliminary → Confirmed.
    ///
    /// Strategy: pre-flight check via <c>GetReservedSeatIdsAsync</c> picks up the common
    /// race-loss case (a concurrent buyer's webhook beat us). On race-loss, we log + emit
    /// the <c>seat_conversion.race_lost</c> metric per losing seat, do NOT insert any
    /// reservations, do NOT call <c>ConfirmSeatAssignments</c>, and clear the pending
    /// stash so a webhook retry doesn't loop. Registration ends "confirmed-but-unseated";
    /// the S8.4 audit script + ops dashboard alert on this state for manual reseat.
    ///
    /// All-or-nothing semantics: <c>Registration.ConfirmSeatAssignments</c> requires
    /// count match (per S8.1 invariants), so a partial-survivor list isn't safe to bind.
    /// Either every seat survives the pre-flight, or every seat is treated as race-lost.
    /// </summary>
    private async Task ConvertPendingSeatAssignmentsAsync(
        Registration registration,
        Guid correlationId,
        CancellationToken ct)
    {
        var pending = registration.PendingSeatAssignments;
        var seatIds = pending.Select(p => p.SeatId).ToList();

        _logger.LogInformation(
            "[Phase 8 S8.2.C] [Webhook-SeatConversion-1] Converting pending seats - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, EventId: {EventId}, SessionId: {SessionId}, SeatCount: {SeatCount}",
            correlationId, registration.Id, registration.EventId, registration.PendingSeatSessionId ?? "(null)", pending.Count);

        // Pre-flight: any of these seats already reserved by a concurrent buyer?
        var alreadyReservedIds = await _seatReservationRepository.GetReservedSeatIdsAsync(seatIds, ct);

        if (alreadyReservedIds.Count > 0)
        {
            // Race-lost path. Emit metric per losing seat, do NOT insert anything.
            // Registration ends confirmed-but-unseated; S8.4 audit handles cleanup.
            foreach (var lostSeatId in alreadyReservedIds)
            {
                _seatHoldMetrics.SeatConversionRaceLost(registration.EventId, registration.Id, lostSeatId);
            }

            _logger.LogWarning(
                "[Phase 8 S8.2.C] [Webhook-SeatConversion-RaceLost] {LostCount}/{TotalCount} seat(s) lost to concurrent buyers — registration ends confirmed-but-unseated. CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
                alreadyReservedIds.Count, pending.Count, correlationId, registration.Id, registration.EventId);

            registration.ClearPendingSeatAssignments();
            return;
        }

        // All-clear path: insert reservations, confirm holds, bind attendees.
        var reservationsToInsert = new List<SeatReservation>(pending.Count);
        foreach (var p in pending)
        {
            var reservationResult = SeatReservation.Create(
                seatId: p.SeatId,
                registrationId: registration.Id,
                eventId: registration.EventId,
                attendeeIndex: p.AttendeeIndex);

            if (reservationResult.IsFailure)
            {
                // Should not happen — invariants on PendingSeatAssignment guarantee
                // valid input here. If it does, treat as fatal-to-conversion + clear stash.
                _logger.LogError(
                    "[Phase 8 S8.2.C] [Webhook-SeatConversion-ERROR] SeatReservation.Create failed (should be impossible) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, SeatId: {SeatId}, Error: {Error}",
                    correlationId, registration.Id, p.SeatId, reservationResult.Error);
                registration.ClearPendingSeatAssignments();
                return;
            }
            reservationsToInsert.Add(reservationResult.Value);
        }

        await _seatReservationRepository.AddRangeAsync(reservationsToInsert, ct);

        // Confirm matching SeatHolds. Best-effort — hold may have expired by the
        // time the webhook fires (Stripe checkout window > 10-min hold TTL). Failure
        // here is non-fatal: the reservation row is the source of truth.
        if (!string.IsNullOrEmpty(registration.PendingSeatSessionId))
        {
            try
            {
                var holds = await _seatHoldRepository.GetActiveHoldsBySessionAsync(
                    registration.PendingSeatSessionId, ct);
                var heldSeatIdSet = pending.Select(p => p.SeatId).ToHashSet();

                foreach (var hold in holds)
                {
                    if (!heldSeatIdSet.Contains(hold.SeatId))
                        continue;

                    var confirmResult = hold.Confirm();
                    if (confirmResult.IsFailure)
                    {
                        _logger.LogInformation(
                            "[Phase 8 S8.2.C] [Webhook-SeatConversion-HoldConfirm] Skipping hold {HoldId} (status={Status}) - CorrelationId: {CorrelationId}",
                            hold.Id, hold.Status, correlationId);
                        continue;
                    }
                    _seatHoldRepository.Update(hold);
                }
            }
            catch (Exception holdEx)
            {
                // Hold confirmation is observability-only; the reservation row is
                // authoritative. Log and continue.
                _logger.LogWarning(holdEx,
                    "[Phase 8 S8.2.C] [Webhook-SeatConversion-WARN] Failed to confirm seat holds (non-fatal). CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, SessionId: {SessionId}",
                    correlationId, registration.Id, registration.PendingSeatSessionId);
            }
        }

        // Bind seat-ids + labels onto each AttendeeDetails (raises SeatsReservedEvent
        // on first successful binding; idempotent on retry).
        var assignments = pending
            .Select(p => (p.AttendeeIndex, p.SeatId, p.SeatLabel))
            .ToList();
        IReadOnlyList<(int, Guid, string)> assignmentsRO = assignments;
        var bindResult = registration.ConfirmSeatAssignments(assignmentsRO);
        if (bindResult.IsFailure)
        {
            // Architect risk register R4: webhook treats failure as logged warning, not fatal.
            _logger.LogWarning(
                "[Phase 8 S8.2.C] [Webhook-SeatConversion-WARN] ConfirmSeatAssignments failed (NOT fatal). CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, Error: {Error}",
                correlationId, registration.Id, bindResult.Error);
        }

        // Always clear the pending stash on success — defence against replay loops.
        registration.ClearPendingSeatAssignments();

        _seatHoldMetrics.SeatHoldConvertedToReservation(
            registration.EventId, registration.Id, pending.Count);

        _logger.LogInformation(
            "[Phase 8 S8.2.C] [Webhook-SeatConversion-SUCCESS] Converted {SeatCount} pending seats to reservations + bound to attendees. CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
            pending.Count, correlationId, registration.Id, registration.EventId);
    }
}
