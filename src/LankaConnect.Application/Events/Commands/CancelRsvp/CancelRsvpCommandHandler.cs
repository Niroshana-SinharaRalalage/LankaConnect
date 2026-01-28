using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

public class CancelRsvpCommandHandler : ICommandHandler<CancelRsvpCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelRsvpCommandHandler> _logger;

    public CancelRsvpCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IStripePaymentService stripePaymentService,
        IUnitOfWork unitOfWork,
        ILogger<CancelRsvpCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _stripePaymentService = stripePaymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelRsvpCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CancelRsvp"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CancelRsvp START: EventId={EventId}, UserId={UserId}, DeleteCommitments={DeleteCommitments}",
                request.EventId, request.UserId, request.DeleteSignUpCommitments);

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

                    return Result.Failure("Event not found");
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

                    return Result.Failure("Cannot cancel registration after the event has started");
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
                    // This follows REST API idempotency best practices: DELETE operations should be idempotent
                    return Result.Success();
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

                    return Result.Failure("Failed to cancel registration");
                }

                using (LogContext.PushProperty("RegistrationId", registration.Id))
                {

                    // Phase 6A.28: Handle sign-up commitments based on user choice
                    // Fix: Trust domain model as single source of truth (removed competing deletion strategies)
                    if (request.DeleteSignUpCommitments)
                    {
                        _logger.LogInformation(
                            "CancelRsvp: Deleting commitments via domain model - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);

                        var cancelResult = @event.CancelAllUserCommitments(request.UserId);

                        if (cancelResult.IsFailure)
                        {
                            _logger.LogWarning(
                                "CancelRsvp: Failed to delete commitments - EventId={EventId}, UserId={UserId}, Error={Error}",
                                request.EventId, request.UserId, cancelResult.Error);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "CancelRsvp: Commitments cancelled successfully - EventId={EventId}, UserId={UserId}",
                                request.EventId, request.UserId);
                        }

                        // CRITICAL FIX ADR-007: Explicitly mark event as modified for EF Core change tracking
                        // Without this, collection deletions (commitments removed) are not tracked even though
                        // domain method executed successfully. Pattern matches RsvpToEventCommandHandler (Phase 6A.24)
                        _eventRepository.Update(@event);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "CancelRsvp: User chose to keep sign-up commitments - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);
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
                            return Result.Failure(abandonResult.Error);
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
                        // Phase 6A.91: Paid confirmed registration - initiate refund workflow
                        _logger.LogInformation(
                            "[Phase 6A.91] Initiating refund workflow for paid registration - RegId={RegId}, EventId={EventId}, UserId={UserId}, PaymentIntentId={PaymentIntentId}",
                            registration.Id, request.EventId, request.UserId, registration.StripePaymentIntentId);

                        // Validate PaymentIntentId exists for Stripe refund
                        if (string.IsNullOrWhiteSpace(registration.StripePaymentIntentId))
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "[Phase 6A.91] Cannot process refund: Missing StripePaymentIntentId - RegId={RegId}, Duration={ElapsedMs}ms",
                                registration.Id, stopwatch.ElapsedMilliseconds);
                            return Result.Failure("Cannot process refund: Payment information not found. Please contact support.");
                        }

                        // Initiate Stripe refund
                        var refundRequest = new CreateRefundRequest
                        {
                            PaymentIntentId = registration.StripePaymentIntentId,
                            RegistrationId = registration.Id,
                            AmountInCents = registration.TotalPrice != null
                                ? (long)(registration.TotalPrice.Amount * 100)
                                : null,
                            Reason = "requested_by_customer",
                            Metadata = new Dictionary<string, string>
                            {
                                ["event_id"] = request.EventId.ToString(),
                                ["user_id"] = request.UserId.ToString(),
                                ["event_title"] = @event.Title.Value
                            }
                        };

                        var refundResult = await _stripePaymentService.CreateRefundAsync(refundRequest, cancellationToken);

                        if (refundResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "[Phase 6A.91] Stripe refund failed - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                                registration.Id, refundResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result.Failure($"Refund failed: {refundResult.Error}");
                        }

                        _logger.LogInformation(
                            "[Phase 6A.91] Stripe refund initiated successfully - RegId={RegId}, StripeRefundId={RefundId}, Status={Status}",
                            registration.Id, refundResult.Value.RefundId, refundResult.Value.Status);

                        // Transition registration to RefundRequested state
                        var requestRefundResult = registration.RequestRefund();
                        if (requestRefundResult.IsFailure)
                        {
                            stopwatch.Stop();
                            _logger.LogError(
                                "[Phase 6A.91] Failed to transition to RefundRequested - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                                registration.Id, requestRefundResult.Error, stopwatch.ElapsedMilliseconds);
                            return Result.Failure(requestRefundResult.Error);
                        }

                        _registrationRepository.Update(registration);

                        _logger.LogInformation(
                            "[Phase 6A.91] Registration transitioned to RefundRequested - RegId={RegId}, EventId={EventId}, UserId={UserId}",
                            registration.Id, request.EventId, request.UserId);

                        // Note: RefundRequestedEvent is raised by domain method, triggers email notification
                    }
                    else
                    {
                        // Free confirmed/other registrations: Hard delete (existing behavior from Phase 6A.45)
                        _logger.LogInformation(
                            "CancelRsvp: Hard deleting registration - RegId={RegId}, EventId={EventId}, UserId={UserId}, Status={Status}, PaymentStatus={PaymentStatus}",
                            registration.Id, request.EventId, request.UserId, registration.Status, registration.PaymentStatus);

                        _registrationRepository.Remove(registration);

                        // Phase 6A.62 Fix: Raise domain event for email notification
                        // The original CancelRegistration domain method was bypassed due to EF Core navigation issues,
                        // but we still need to trigger the email notification via the domain event.
                        @event.RaiseRegistrationCancelledEvent(request.UserId);
                        _eventRepository.Update(@event);

                        _logger.LogInformation(
                            "CancelRsvp: Raised RegistrationCancelledEvent for email notification - EventId={EventId}, UserId={UserId}",
                            request.EventId, request.UserId);
                    }

                    // Save changes
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CancelRsvp COMPLETE: EventId={EventId}, UserId={UserId}, RegId={RegId}, DeletedCommitments={DeletedCommitments}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, registration.Id, request.DeleteSignUpCommitments, stopwatch.ElapsedMilliseconds);

                    return Result.Success();
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
