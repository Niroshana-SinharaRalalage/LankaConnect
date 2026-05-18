using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Organizer rejects a pending refund request. Returns the Registration
/// to Confirmed. Rejection reason is mandatory (customer-facing in rejection email).
/// </summary>
public class RejectRefundRequestCommandHandler : ICommandHandler<RejectRefundRequestCommand>
{
    private readonly IEventRepository _eventRepo;
    private readonly IRegistrationRepository _registrationRepo;
    private readonly IRefundRequestRepository _refundRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RejectRefundRequestCommandHandler> _logger;

    public RejectRefundRequestCommandHandler(
        IEventRepository eventRepo,
        IRegistrationRepository registrationRepo,
        IRefundRequestRepository refundRepo,
        IUnitOfWork uow,
        ILogger<RejectRefundRequestCommandHandler> logger)
    {
        _eventRepo = eventRepo;
        _registrationRepo = registrationRepo;
        _refundRepo = refundRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result> Handle(
        RejectRefundRequestCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RejectRefundRequest"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("RefundRequestId", request.RefundRequestId))
        using (LogContext.PushProperty("CallerUserId", request.CallerUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148] RejectRefundRequest START: EventId={EventId} RrId={RrId} CallerUserId={UserId}",
                request.EventId, request.RefundRequestId, request.CallerUserId);

            try
            {
                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                    return Result.Failure("RejectionReason is required");

                var @event = await _eventRepo.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null) return Result.NotFound("Event not found");
                if (!@event.IsOrganizer(request.CallerUserId))
                    return Result.Forbidden("Only organizers of this event can reject refund requests");

                // Find the refund request (untracked just to discover RegistrationId).
                var locator = await _refundRepo.GetByIdAsync(request.RefundRequestId, cancellationToken);
                if (locator is null)
                    return Result.NotFound("Refund request not found");

                // Load tracked parent registration (includes RefundRequests + LineItems).
                var tracked = await _registrationRepo.GetByIdAsync(locator.RegistrationId, cancellationToken);
                if (tracked is null)
                    return Result.Failure("Failed to load registration for update");

                if (tracked.EventId != request.EventId)
                    return Result.NotFound("Refund request does not belong to this event");

                var trackedRequest = tracked.RefundRequests.FirstOrDefault(r => r.Id == request.RefundRequestId);
                if (trackedRequest is null)
                    return Result.NotFound("Refund request not found on registration");

                var rejectResult = trackedRequest.Reject(request.CallerUserId, request.RejectionReason);
                if (rejectResult.IsFailure)
                {
                    _logger.LogWarning(
                        "[6A.148] RejectRefundRequest Validation FAILED: RrId={RrId} Error={Error}",
                        request.RefundRequestId, rejectResult.Error);
                    return Result.Failure(rejectResult.Error);
                }

                // Phase 6A.148 post-rework: cancel + refund are decoupled. Rejection
                // marks the RefundRequest as Rejected but does NOT mutate the registration.
                // - If registration was Confirmed (standalone refund path), it stays Confirmed.
                // - If registration was Cancelled (cancel+refund compound path), it stays
                //   Cancelled — per product decision: rejection does not restore the seat.

                _registrationRepo.Update(tracked);
                await _uow.CommitAsync(cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "[6A.148] RejectRefundRequest COMPLETE: RrId={RrId} RegId={RegId} Duration={ElapsedMs}ms",
                    request.RefundRequestId, tracked.Id, sw.ElapsedMilliseconds);
                return Result.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148] RejectRefundRequest EXCEPTION: RrId={RrId} Duration={ElapsedMs}ms",
                    request.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
