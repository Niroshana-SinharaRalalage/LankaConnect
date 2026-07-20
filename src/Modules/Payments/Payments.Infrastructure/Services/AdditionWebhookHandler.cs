using LankaConnect.Products.LankaEvents.Contracts.Repositories;
using LankaConnect.Products.LankaEvents.Contracts.Services;
using LankaConnect.Products.LankaEvents.Contracts.DTOs;
using LankaConnect.Products.LankaEvents.Contracts.Shims; // Wave 6.5.g Day 5
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Phase 0: Handles Stripe webhook events for add-only attendee (addition) payments.
/// Extracted from PaymentsController for separation of concerns.
/// </summary>
public class AdditionWebhookHandler : IAdditionWebhookHandler
{
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IRegistrationPaymentRepository _paymentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdditionWebhookHandler> _logger;

    public AdditionWebhookHandler(
        IRegistrationAdditionRepository additionRepository,
        IRegistrationRepository registrationRepository,
        IRegistrationPaymentRepository paymentRepository,
        IEventRepository eventRepository,
        IRevenueCalculatorService revenueCalculatorService,
        IUnitOfWork unitOfWork,
        ILogger<AdditionWebhookHandler> logger)
    {
        _additionRepository = additionRepository;
        _registrationRepository = registrationRepository;
        _paymentRepository = paymentRepository;
        _eventRepository = eventRepository;
        _revenueCalculatorService = revenueCalculatorService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-1] Processing addition payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
            correlationId, sessionId);

        // Extract metadata with Phase 6A.136D fallback to session ID lookup
        if (!metadata.TryGetValue("registration_addition_id", out var additionIdStr) ||
            !Guid.TryParse(additionIdStr, out var additionId))
        {
            _logger.LogWarning(
                "[AddOnlyAttendees] [Webhook-Addition-WARN] Missing registration_addition_id in metadata, attempting session ID fallback - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            // Phase 6A.136D: Fallback — lookup RegistrationAddition by Stripe session ID
            var fallbackAddition = await _additionRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (fallbackAddition == null)
            {
                _logger.LogError(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Fallback failed - no RegistrationAddition for SessionId - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            additionId = fallbackAddition.Id;
            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-1b] Fallback succeeded - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, SessionId: {SessionId}",
                correlationId, additionId, sessionId);
        }

        if (!metadata.TryGetValue("registration_id", out var registrationIdStr) ||
            !Guid.TryParse(registrationIdStr, out var registrationId))
        {
            _logger.LogWarning(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Missing registration_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);
            return;
        }

        if (!metadata.TryGetValue("event_id", out var eventIdStr) ||
            !Guid.TryParse(eventIdStr, out var eventId))
        {
            _logger.LogWarning(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Missing event_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);
            return;
        }

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-2] Metadata extracted - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
            correlationId, additionId, registrationId, eventId);

        // Load the RegistrationAddition (with tracking)
        var addition = await _additionRepository.GetByIdAsync(additionId);
        if (addition == null)
        {
            _logger.LogError(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] RegistrationAddition not found - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}",
                correlationId, additionId);
            return;
        }

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-3] Addition loaded - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, Status: {Status}, NewAttendeesCount: {Count}",
            correlationId, additionId, addition.Status, addition.NewAttendees.Count);

        // Verify the addition is still pending
        if (addition.Status != RegistrationAdditionStatus.Pending)
        {
            _logger.LogWarning(
                "[AddOnlyAttendees] [Webhook-Addition-WARN] Addition not in Pending status - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, CurrentStatus: {Status}",
                correlationId, additionId, addition.Status);
            return;
        }

        // Load the registration (with tracking)
        var registration = await _registrationRepository.GetByIdAsync(registrationId);
        if (registration == null)
        {
            _logger.LogError(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Registration not found - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                correlationId, registrationId);
            return;
        }

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-4] Registration loaded - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentAttendees: {Count}, PaymentStatus: {PaymentStatus}",
            correlationId, registrationId, registration.Attendees.Count, registration.PaymentStatus);

        // Complete payment on the addition
        var completePaymentResult = addition.CompletePayment(paymentIntentId);
        if (completePaymentResult.IsFailure)
        {
            _logger.LogError(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, Error: {Error}",
                correlationId, additionId, completePaymentResult.Error);
            return;
        }

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-5] Payment completed on addition - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, PaymentIntentId: {PaymentIntentId}",
            correlationId, additionId, paymentIntentId);

        // Get the event to determine max attendees per registration
        var @event = await _eventRepository.GetByIdAsync(eventId);
        var maxAttendeesPerRegistration = @event?.MaxAttendeesPerRegistration ?? 10;

        // Create RegistrationPayment record for audit trail (needed for AddAttendees)
        var paymentResult = RegistrationPayment.CreateAddition(
            registrationId,
            paymentIntentId,
            addition.AdditionalAmount,
            additionId,
            PaymentStatus.Completed);

        if (paymentResult.IsFailure)
        {
            _logger.LogError(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Failed to create payment record - CorrelationId: {CorrelationId}, Error: {Error}",
                correlationId, paymentResult.Error);
            addition.MarkAsFailed();
            _additionRepository.Update(addition);
            await _unitOfWork.CommitAsync();
            return;
        }

        var payment = paymentResult.Value;
        await _paymentRepository.AddAsync(payment);

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-6] Payment record created - CorrelationId: {CorrelationId}, PaymentId: {PaymentId}, Amount: {Amount}",
            correlationId, payment.Id, addition.AdditionalAmount.Amount);

        // Calculate new total price (previous total + additional amount)
        var previousTotal = registration.TotalPrice?.Amount ?? 0m;
        var newTotalAmount = previousTotal + addition.AdditionalAmount.Amount;
        var newTotalPrice = new LankaConnect.SharedKernel.Money.Money(newTotalAmount, addition.AdditionalAmount.Currency);
        var newTotalPriceResult = LankaConnect.BuildingBlocks.Domain.Result<LankaConnect.SharedKernel.Money.Money>.Success(newTotalPrice);

        if (newTotalPriceResult.IsFailure)
        {
            _logger.LogError(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Failed to create new total price - CorrelationId: {CorrelationId}, Error: {Error}",
                correlationId, newTotalPriceResult.Error);
            addition.MarkAsFailed();
            _additionRepository.Update(addition);
            await _unitOfWork.CommitAsync();
            return;
        }

        // Phase 7F-D (architect-approved 2026-04-30): dispatch by addition mode. Mode A
        // additions go through registration.AddAttendees (per-attendee path); Mode B
        // additions go through registration.MergeHeadCountAddition (head-count axis).
        Result mergeResult;
        if (addition.IsModeBAddition && addition.HeadCountDelta != null)
        {
            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-6b] Mode-B head-count merge — CorrelationId: {CorrelationId}, AdditionMode: {Mode}, DeltaTotal: {Total}",
                correlationId, addition.RegistrationMode, addition.HeadCountDelta.Total);
            mergeResult = registration.MergeHeadCountAddition(
                additionMode: addition.RegistrationMode,
                headCountDelta: addition.HeadCountDelta,
                newTotalPrice: newTotalPriceResult.Value,
                maxAttendeesPerRegistration: maxAttendeesPerRegistration);
        }
        else
        {
            mergeResult = registration.AddAttendees(
                addition.NewAttendees,
                newTotalPriceResult.Value,
                payment,
                additionId,
                maxAttendeesPerRegistration);
        }

        if (mergeResult.IsFailure)
        {
            _logger.LogError(
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Merge failed (Mode={Mode}) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, Error: {Error}",
                addition.RegistrationMode, correlationId, registrationId, mergeResult.Error);
            addition.MarkAsFailed();
            _additionRepository.Update(addition);
            await _unitOfWork.CommitAsync();
            return;
        }

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-7] Merge complete (Mode={Mode}) — CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewAttendeeCount: {NewCount}, PostMergeTotal: {Total}",
            addition.RegistrationMode, correlationId, registrationId,
            addition.IsModeBAddition ? addition.HeadCountDelta!.Total : addition.NewAttendees.Count,
            addition.IsModeBAddition ? registration.HeadCount?.Total ?? 0 : registration.Attendees.Count);

        // Phase 6A.X FIX: Recalculate revenue breakdown for cumulative total
        try
        {
            var eventForBreakdown = await _eventRepository.GetByIdAsync(eventId);
            if (eventForBreakdown?.Location != null)
            {
                _logger.LogInformation(
                    "[AddOnlyAttendees] [Webhook-Addition-7b] Recalculating revenue breakdown for cumulative total - CorrelationId: {CorrelationId}, NewTotalPrice: {TotalPrice}",
                    correlationId, newTotalPriceResult.Value.Amount);

                var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                    newTotalPriceResult.Value,
                    eventForBreakdown.Location,
                    CancellationToken.None);

                if (breakdownResult.IsSuccess)
                {
                    registration.SetRevenueBreakdown(breakdownResult.Value);
                    _logger.LogInformation(
                        "[AddOnlyAttendees] [Webhook-Addition-7c] Revenue breakdown updated - CorrelationId: {CorrelationId}, GrossRevenue: {Gross}, StripeFee: {StripeFee}, PlatformCommission: {Commission}, OrganizerPayout: {Payout}",
                        correlationId,
                        newTotalPriceResult.Value.Amount,
                        breakdownResult.Value.StripeFeeAmount.Amount,
                        breakdownResult.Value.PlatformCommission.Amount,
                        breakdownResult.Value.OrganizerPayout.Amount);
                }
                else
                {
                    _logger.LogWarning(
                        "[AddOnlyAttendees] [Webhook-Addition-WARN] Revenue breakdown calculation failed - CorrelationId: {CorrelationId}, Error: {Error}",
                        correlationId, breakdownResult.Error);
                }
            }
            else
            {
                _logger.LogWarning(
                    "[AddOnlyAttendees] [Webhook-Addition-WARN] Event or location not found for breakdown calculation - CorrelationId: {CorrelationId}, EventId: {EventId}",
                    correlationId, eventId);
            }
        }
        catch (Exception breakdownEx)
        {
            // Don't fail the whole operation if breakdown calculation fails
            _logger.LogError(breakdownEx,
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Exception during revenue breakdown calculation - CorrelationId: {CorrelationId}",
                correlationId);
        }

        // Mark addition as merged
        var markMergedResult = addition.MarkAsMerged();
        if (markMergedResult.IsFailure)
        {
            _logger.LogWarning(
                "[AddOnlyAttendees] [Webhook-Addition-WARN] MarkAsMerged failed but continuing - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, Error: {Error}",
                correlationId, additionId, markMergedResult.Error);
        }

        // Save all changes and dispatch domain events
        _additionRepository.Update(addition);
        _registrationRepository.Update(registration);

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-8] Before CommitAsync - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, RegistrationDomainEvents: {Count}",
            correlationId, additionId, registration.DomainEvents.Count);

        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "[AddOnlyAttendees] [Webhook-Addition-SUCCESS] Addition completed successfully - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, AdditionId: {AdditionId}, PaymentIntentId: {PaymentIntentId}, NewAttendees: {NewAttendees}, TotalAttendees: {TotalAttendees}",
            correlationId, eventId, registrationId, additionId, paymentIntentId, addition.NewAttendees.Count, registration.Attendees.Count);
    }

    /// <summary>
    /// Phase 6A.136 Issue #2: Handles checkout.session.expired for addition payments.
    /// Marks the RegistrationAddition as Abandoned to prevent Pending entities from leaking.
    /// </summary>
    public async Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 6A.136] [Webhook-Addition-Expired-1] Processing addition session expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            if (!metadata.TryGetValue("registration_addition_id", out var additionIdStr) ||
                !Guid.TryParse(additionIdStr, out var additionId))
            {
                _logger.LogWarning(
                    "[Phase 6A.136] [Webhook-Addition-Expired-WARN] Missing registration_addition_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            var addition = await _additionRepository.GetByIdAsync(additionId);
            if (addition == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.136] [Webhook-Addition-Expired-WARN] RegistrationAddition not found - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}",
                    correlationId, additionId);
                return;
            }

            var abandonResult = addition.MarkAsAbandoned();
            if (abandonResult.IsFailure)
            {
                _logger.LogWarning(
                    "[Phase 6A.136] [Webhook-Addition-Expired-WARN] MarkAsAbandoned failed - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, Error: {Error}",
                    correlationId, additionId, abandonResult.Error);
                return;
            }

            _additionRepository.Update(addition);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Phase 6A.136] [Webhook-Addition-Expired-SUCCESS] Addition marked as Abandoned - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}",
                correlationId, additionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.136] [Webhook-Addition-Expired-ERROR] Error handling addition expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);
        }
    }
}
