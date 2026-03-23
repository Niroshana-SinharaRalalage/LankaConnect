using System.Diagnostics;
using System.Linq;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

public class CancelRsvpCommandHandler : ICommandHandler<CancelRsvpCommand, CancelRsvpResult>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IRegistrationRefundService _refundService;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly IAddOnRefundService _addOnRefundService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelRsvpCommandHandler> _logger;

    public CancelRsvpCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IRegistrationRefundService refundService,
        IFormResponseRepository formResponseRepository,
        IAddOnRefundService addOnRefundService,
        IUnitOfWork unitOfWork,
        ILogger<CancelRsvpCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _refundService = refundService;
        _formResponseRepository = formResponseRepository;
        _addOnRefundService = addOnRefundService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CancelRsvpResult>> Handle(CancelRsvpCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CancelRsvp"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CancelRsvp START: EventId={EventId}, UserId={UserId}, DeleteCommitments={DeleteCommitments}, DeleteFormResponses={DeleteFormResponses}, RefundAddOnPurchases={RefundAddOnPurchases}",
                request.EventId, request.UserId, request.DeleteSignUpCommitments, request.DeleteFormResponses, request.RefundAddOnPurchases);

            try
            {
                // Verify event exists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelRsvp FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "CancelRsvp: Event loaded - EventId={EventId}, Title={Title}, Status={Status}",
                    @event.Id, @event.Title.Value, @event.Status);

                // Phase 6A.91: Check if event has started - cannot cancel after event starts
                if (@event.StartDate <= DateTime.UtcNow)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "[Phase 6A.91] CancelRsvp FAILED: Cannot cancel after event has started - EventId={EventId}, StartDate={StartDate}, Now={Now}, Duration={ElapsedMs}ms",
                        request.EventId, @event.StartDate, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Failure("Cannot cancel registration after the event has started");
                }

                // Find active registration using GetByEventAndUserAsync (read-only query)
                var registrationReadOnly = await _registrationRepository.GetByEventAndUserAsync(request.EventId, request.UserId, cancellationToken);

                _logger.LogInformation(
                    "CancelRsvp: Registration query result - Found={Found}, Status={Status}",
                    registrationReadOnly != null, registrationReadOnly?.Status.ToString() ?? "N/A");

                if (registrationReadOnly == null)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CancelRsvp COMPLETE: No registration found (idempotent) - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                    // Phase 6A.45: Since we hard delete, no registration found means operation already succeeded (idempotent)
                    return Result<CancelRsvpResult>.Success(new CancelRsvpResult(
                        RegistrationCancelled: true,
                        CommitmentsDeleted: null,
                        FormResponsesDeleted: null,
                        FormResponsesDeletedCount: null,
                        AddOnRefundsProcessed: null,
                        AddOnRefundedCount: null,
                        AddOnFailedCount: null,
                        AddOnRefundTotal: null,
                        Warnings: null));
                }

                // Phase 6A.45 FIX: Hard delete registration instead of soft delete (marking as cancelled)
                // This prevents duplicate/cancelled registrations from cluttering the database
                // Get the registration WITH tracking so EF Core can delete it
                var registration = await _registrationRepository.GetByIdAsync(registrationReadOnly.Id, cancellationToken);

                if (registration == null)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "CancelRsvp FAILED: Could not retrieve registration with tracking - RegId={RegId}, Duration={ElapsedMs}ms",
                        registrationReadOnly.Id, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Failure("Failed to cancel registration");
                }

                using (LogContext.PushProperty("RegistrationId", registration.Id))
                {
                    // Track warnings for partial failures
                    var warnings = new List<string>();

                    // Phase 6A.28: Handle sign-up commitments based on user choice
                    bool? commitmentsDeleted = null;
                    if (request.DeleteSignUpCommitments)
                    {
                        _logger.LogInformation(
                            "CancelRsvp: Deleting commitments via domain model - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);

                        var cancelResult = @event.CancelAllUserCommitments(request.UserId);

                        if (cancelResult.IsFailure)
                        {
                            commitmentsDeleted = false;
                            warnings.Add($"Failed to delete sign-up commitments: {cancelResult.Error}");
                            _logger.LogWarning(
                                "CancelRsvp: Failed to delete commitments - EventId={EventId}, UserId={UserId}, Error={Error}",
                                request.EventId, request.UserId, cancelResult.Error);
                        }
                        else
                        {
                            commitmentsDeleted = true;
                            _logger.LogInformation(
                                "CancelRsvp: Commitments cancelled successfully - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);
                        }

                        // CRITICAL FIX ADR-007: Explicitly mark event as modified for EF Core change tracking
                        _eventRepository.Update(@event);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "CancelRsvp: User chose to keep sign-up commitments - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);
                    }

                    // Cancellation enhancement: Handle form response deletion (non-blocking)
                    bool? formResponsesDeleted = null;
                    int? formResponsesDeletedCount = null;
                    if (request.DeleteFormResponses)
                    {
                        try
                        {
                            _logger.LogInformation(
                                "CancelRsvp: Deleting form responses - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);

                            var formResponses = await _formResponseRepository.GetByEventAndUserAsync(
                                request.EventId, request.UserId, cancellationToken);

                            if (formResponses.Count > 0)
                            {
                                foreach (var response in formResponses)
                                {
                                    response.RaiseDeletedEvent();
                                    _formResponseRepository.Remove(response);
                                }

                                formResponsesDeleted = true;
                                formResponsesDeletedCount = formResponses.Count;

                                _logger.LogInformation(
                                    "CancelRsvp: Deleted {Count} form responses - EventId={EventId}, UserId={UserId}",
                                    formResponses.Count, request.EventId, request.UserId);
                            }
                            else
                            {
                                formResponsesDeleted = true;
                                formResponsesDeletedCount = 0;

                                _logger.LogInformation(
                                    "CancelRsvp: No form responses found to delete - EventId={EventId}, UserId={UserId}",
                                    request.EventId, request.UserId);
                            }
                        }
                        catch (Exception ex)
                        {
                            formResponsesDeleted = false;
                            warnings.Add($"Failed to delete form responses: {ex.Message}");
                            _logger.LogError(ex,
                                "CancelRsvp: Failed to delete form responses (non-blocking) - EventId={EventId}, UserId={UserId}, Error={ErrorMessage}",
                                request.EventId, request.UserId, ex.Message);
                        }
                    }

                    // Cancellation enhancement: Handle add-on purchase refunds BEFORE registration refund
                    // so the add-on total can be included in the refund email
                    decimal addOnRefundTotal = 0m;
                    int addOnRefundedCount = 0;
                    int addOnFailedCount = 0;
                    bool? addOnRefundsProcessed = null;

                    if (request.RefundAddOnPurchases)
                    {
                        try
                        {
                            _logger.LogInformation(
                                "CancelRsvp: Refunding add-on purchases - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);

                            var addOnMetadata = new Dictionary<string, string>
                            {
                                ["event_id"] = request.EventId.ToString(),
                                ["user_id"] = request.UserId.ToString(),
                                ["refund_type"] = "user_cancellation_add_on_refund"
                            };

                            var addOnRefundResult = await _addOnRefundService.RefundUserPurchasesAsync(
                                request.UserId,
                                request.EventId,
                                "requested_by_customer",
                                addOnMetadata,
                                cancellationToken);

                            if (addOnRefundResult.IsFailure)
                            {
                                addOnRefundsProcessed = false;
                                warnings.Add($"Add-on refund failed: {addOnRefundResult.Error}");
                                _logger.LogWarning(
                                    "CancelRsvp: Add-on refund failed (non-blocking) - EventId={EventId}, UserId={UserId}, Error={Error}",
                                    request.EventId, request.UserId, addOnRefundResult.Error);
                            }
                            else
                            {
                                addOnRefundTotal = addOnRefundResult.Value.TotalAmountRefunded;
                                addOnRefundedCount = addOnRefundResult.Value.PurchasesRefunded;
                                addOnFailedCount = addOnRefundResult.Value.PurchasesFailed;
                                addOnRefundsProcessed = addOnFailedCount == 0;

                                if (addOnFailedCount > 0)
                                {
                                    warnings.Add($"{addOnFailedCount} add-on purchase(s) failed to refund. {addOnRefundedCount} succeeded (${addOnRefundTotal:F2} refunded).");
                                }

                                _logger.LogInformation(
                                    "CancelRsvp: Add-on refunds processed - EventId={EventId}, UserId={UserId}, Refunded={Refunded}, Failed={Failed}, TotalAmount=${TotalAmount}",
                                    request.EventId, request.UserId,
                                    addOnRefundedCount, addOnFailedCount, addOnRefundTotal);
                            }
                        }
                        catch (Exception ex)
                        {
                            addOnRefundsProcessed = false;
                            warnings.Add($"Add-on refund error: {ex.Message}");
                            _logger.LogError(ex,
                                "CancelRsvp: Failed to refund add-on purchases (non-blocking) - EventId={EventId}, UserId={UserId}, Error={ErrorMessage}",
                                request.EventId, request.UserId, ex.Message);
                        }
                    }

                    // Phase 6A.81 Part 3: Different handling for Preliminary vs Confirmed registrations
                    if (registration.Status == RegistrationStatus.Preliminary)
                    {
                        // Preliminary registrations: Mark as Abandoned (preserve audit trail)
                        _logger.LogInformation(
                            "[Phase 6A.81-Part3] Marking Preliminary registration as Abandoned - RegId={RegId}, EventId={EventId}, UserId={UserId}",
                            registration.Id, request.EventId, request.UserId);

                        var abandonResult = registration.MarkAbandoned();
                        if (abandonResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "[Phase 6A.81-Part3] Failed to abandon Preliminary registration - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                                registration.Id, abandonResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result<CancelRsvpResult>.Failure(abandonResult.Error);
                        }

                        _registrationRepository.Update(registration);

                        _logger.LogInformation(
                            "[Phase 6A.81-Part3] Preliminary registration marked as Abandoned successfully - RegId={RegId}, EventId={EventId}, UserId={UserId}",
                            registration.Id, request.EventId, request.UserId);

                        // No cancellation email for Preliminary (they never got confirmation email)
                    }
                    else if (registration.Status == RegistrationStatus.Confirmed &&
                             registration.PaymentStatus == PaymentStatus.Completed)
                    {
                        // Phase 6A.92: Paid confirmed registration - use shared refund service
                        _logger.LogInformation(
                            "[Phase 6A.92] Initiating refund via shared service - RegId={RegId}, EventId={EventId}, UserId={UserId}",
                            registration.Id, request.EventId, request.UserId);

                        var metadata = new Dictionary<string, string>
                        {
                            ["event_id"] = request.EventId.ToString(),
                            ["user_id"] = request.UserId.ToString(),
                            ["event_title"] = @event.Title.Value,
                            ["refund_type"] = "user_initiated_cancellation"
                        };

                        // Use shared refund service - handles Stripe call and RequestRefund() transition
                        // Pass addOnRefundTotal so the refund email includes the combined total
                        var refundResult = await _refundService.ProcessRefundAsync(
                            registration,
                            "requested_by_customer",
                            metadata,
                            addOnRefundTotal,
                            cancellationToken);

                        if (refundResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "[Phase 6A.92] Refund failed - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                                registration.Id, refundResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result<CancelRsvpResult>.Failure($"Refund failed: {refundResult.Error}");
                        }

                        _registrationRepository.Update(registration);

                        _logger.LogInformation(
                            "[Phase 6A.92] Refund request successful - RegId={RegId}, StripeRefundId={RefundId}, Amount=${Amount}. Webhook will complete the refund.",
                            registration.Id, refundResult.Value.StripeRefundId, refundResult.Value.AmountRefunded);

                        // Phase 6A.93: Also raise RegistrationCancelledEvent for paid cancellations
                        @event.RaiseRegistrationCancelledEvent(request.UserId);
                        _eventRepository.Update(@event);

                        _logger.LogInformation(
                            "[Phase 6A.93] Raised RegistrationCancelledEvent for cancellation email - EventId={EventId}, UserId={UserId}. User will receive two emails: cancellation + refund.",
                            request.EventId, request.UserId);

                        // Issue #56.1 Diagnostic: Log Event entity's domain event count to verify event was raised
                        _logger.LogInformation(
                            "[Issue #56.1 DIAGNOSTIC] Event entity domain events after RaiseRegistrationCancelledEvent - EventId={EventId}, DomainEventCount={Count}, EventTypes=[{Types}]",
                            @event.Id, @event.DomainEvents.Count,
                            string.Join(", ", @event.DomainEvents.Select(e => e.GetType().Name)));
                    }
                    else
                    {
                        // Free confirmed/other registrations: Hard delete (existing behavior from Phase 6A.45)
                        _logger.LogInformation(
                            "CancelRsvp: Hard deleting registration - RegId={RegId}, EventId={EventId}, UserId={UserId}, Status={Status}, PaymentStatus={PaymentStatus}",
                            registration.Id, request.EventId, request.UserId, registration.Status, registration.PaymentStatus);

                        _registrationRepository.Remove(registration);

                        // Phase 6A.62 Fix: Raise domain event for email notification
                        @event.RaiseRegistrationCancelledEvent(request.UserId);
                        _eventRepository.Update(@event);

                        _logger.LogInformation(
                            "CancelRsvp: Raised RegistrationCancelledEvent for email notification - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);

                        // Issue #56.1 Diagnostic: Log Event entity's domain event count to verify event was raised
                        _logger.LogInformation(
                            "[Issue #56.1 DIAGNOSTIC] Event entity domain events after RaiseRegistrationCancelledEvent (free) - EventId={EventId}, DomainEventCount={Count}, EventTypes=[{Types}]",
                            @event.Id, @event.DomainEvents.Count,
                            string.Join(", ", @event.DomainEvents.Select(e => e.GetType().Name)));
                    }

                    // Save changes
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CancelRsvp COMPLETE: EventId={EventId}, UserId={UserId}, RegId={RegId}, DeletedCommitments={DeletedCommitments}, DeletedFormResponses={DeletedFormResponses}, RefundedAddOns={RefundedAddOns}, Warnings={WarningCount}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, registration.Id, request.DeleteSignUpCommitments, request.DeleteFormResponses, request.RefundAddOnPurchases, warnings.Count, stopwatch.ElapsedMilliseconds);

                    return Result<CancelRsvpResult>.Success(new CancelRsvpResult(
                        RegistrationCancelled: true,
                        CommitmentsDeleted: commitmentsDeleted,
                        FormResponsesDeleted: formResponsesDeleted,
                        FormResponsesDeletedCount: formResponsesDeletedCount,
                        AddOnRefundsProcessed: addOnRefundsProcessed,
                        AddOnRefundedCount: addOnRefundsProcessed.HasValue ? addOnRefundedCount : null,
                        AddOnFailedCount: addOnRefundsProcessed.HasValue ? addOnFailedCount : null,
                        AddOnRefundTotal: addOnRefundsProcessed.HasValue ? addOnRefundTotal : null,
                        Warnings: warnings.Count > 0 ? warnings : null));
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CancelRsvp FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
