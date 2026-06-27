using LankaConnect.Application.Common;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 8 S8.3 — releases a registration's seat reservations back to availability
/// when the registration leaves the "owns the seats" lifecycle states.
///
/// Triggered by <see cref="SeatReservationsReleasedEvent"/> from:
///   - <c>Registration.CompleteRefund</c> (refund_completed)
///   - <c>Registration.MarkAbandoned</c> (checkout_abandoned)
///   - <c>Registration.FailPayment</c> (payment_failed)
///   - <c>Registration.Cancel</c> (registration_cancelled)
///   - <c>Registration.ForceCancelStuckRefund</c> (force_cancelled_stuck_refund)
///
/// V1 cancellation policy is hard-delete (architect ADR-011 Q1) — the seat
/// becomes immediately available to other buyers via the <c>StructuralEditGuard</c>
/// + the new-RSVP <c>SeatAssignmentValidator</c> read paths. Idempotent: calling
/// <c>DeleteByRegistrationIdAsync</c> on a registration with no reservations is a
/// no-op (e.g., the abandonment path on a Preliminary registration where the
/// webhook never converted holds → reservations).
/// </summary>
public class SeatReservationsReleasedEventHandler
    : INotificationHandler<DomainEventNotification<SeatReservationsReleasedEvent>>
{
    private readonly ISeatReservationRepository _seatReservationRepository;
    private readonly ISeatHoldMetrics _seatHoldMetrics;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SeatReservationsReleasedEventHandler> _logger;

    public SeatReservationsReleasedEventHandler(
        ISeatReservationRepository seatReservationRepository,
        ISeatHoldMetrics seatHoldMetrics,
        IUnitOfWork unitOfWork,
        ILogger<SeatReservationsReleasedEventHandler> logger)
    {
        _seatReservationRepository = seatReservationRepository;
        _seatHoldMetrics = seatHoldMetrics;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<SeatReservationsReleasedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "SeatReservationsReleased"))
        using (LogContext.PushProperty("EntityType", "SeatReservation"))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        using (LogContext.PushProperty("Reason", domainEvent.Reason))
        {
            try
            {
                _logger.LogInformation(
                    "[Phase 8 S8.3] [SeatReservationsReleased-1] Releasing reservations - RegistrationId={RegistrationId}, EventId={EventId}, Reason={Reason}",
                    domainEvent.RegistrationId, domainEvent.EventId, domainEvent.Reason);

                // Read first so we can report a meaningful count for the metric.
                var existing = await _seatReservationRepository.GetByRegistrationIdAsync(
                    domainEvent.RegistrationId, cancellationToken);

                if (existing.Count == 0)
                {
                    // Idempotent no-op: typical for Abandoned (Preliminary path
                    // never converted holds → reservations) or for free events.
                    _logger.LogInformation(
                        "[Phase 8 S8.3] [SeatReservationsReleased-NoOp] No reservations to release - RegistrationId={RegistrationId}, Reason={Reason}",
                        domainEvent.RegistrationId, domainEvent.Reason);
                    _seatHoldMetrics.SeatReservationReleased(
                        domainEvent.EventId, domainEvent.RegistrationId, domainEvent.Reason, 0);
                    return;
                }

                await _seatReservationRepository.DeleteByRegistrationIdAsync(
                    domainEvent.RegistrationId, cancellationToken);

                // Separate UoW commit so the deletion is durable even if the
                // outer command transaction has already returned. The triggering
                // domain event was raised AFTER its parent state change committed
                // (event dispatch is post-commit in this codebase).
                await _unitOfWork.CommitAsync();

                _logger.LogInformation(
                    "[Phase 8 S8.3] [SeatReservationsReleased-SUCCESS] Released {Count} reservation(s) - RegistrationId={RegistrationId}, EventId={EventId}, Reason={Reason}",
                    existing.Count, domainEvent.RegistrationId, domainEvent.EventId, domainEvent.Reason);

                _seatHoldMetrics.SeatReservationReleased(
                    domainEvent.EventId, domainEvent.RegistrationId, domainEvent.Reason, existing.Count);
            }
            catch (Exception ex)
            {
                // Don't let a release failure break the parent flow (refund email,
                // cancellation confirmation, etc.). Operator handles via S8.4 audit
                // — registrations whose Status is in {Cancelled, Abandoned, Refunded}
                // but which still have seat_reservations rows are the recovery target.
                _logger.LogError(ex,
                    "[Phase 8 S8.3] [SeatReservationsReleased-ERROR] Failed to release reservations - RegistrationId={RegistrationId}, Reason={Reason}, Error={ErrorMessage}",
                    domainEvent.RegistrationId, domainEvent.Reason, ex.Message);
            }
        }
    }
}
