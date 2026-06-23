using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Organizer approves a pending refund request with per-line approved
/// amounts. After the approval transaction commits, dispatches Stripe via
/// <see cref="IRefundExecutionService"/> (architect F10 ordering — Stripe NEVER runs
/// inside the approve transaction).
///
/// All-zero approvals (architect F2) are rejected with 400 — organizer must use the
/// Reject endpoint instead, which captures a customer-facing rejection reason.
///
/// Concurrency: tracked load + xmin token catches simultaneous approves;
/// <see cref="DbUpdateConcurrencyException"/> is mapped to 409 Conflict.
/// </summary>
public class ApproveRefundRequestCommandHandler : ICommandHandler<ApproveRefundRequestCommand>
{
    private readonly IEventRepository _eventRepo;
    private readonly IRegistrationRepository _registrationRepo;
    private readonly IRefundRequestRepository _refundRepo;
    private readonly IUnitOfWork _uow;
    private readonly IRefundExecutionService _executionService;
    private readonly ILogger<ApproveRefundRequestCommandHandler> _logger;

    public ApproveRefundRequestCommandHandler(
        IEventRepository eventRepo,
        IRegistrationRepository registrationRepo,
        IRefundRequestRepository refundRepo,
        IUnitOfWork uow,
        IRefundExecutionService executionService,
        ILogger<ApproveRefundRequestCommandHandler> logger)
    {
        _eventRepo = eventRepo;
        _registrationRepo = registrationRepo;
        _refundRepo = refundRepo;
        _uow = uow;
        _executionService = executionService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ApproveRefundRequestCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ApproveRefundRequest"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("RefundRequestId", request.RefundRequestId))
        using (LogContext.PushProperty("CallerUserId", request.CallerUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148] ApproveRefundRequest START: EventId={EventId} RrId={RrId} Lines={Count}",
                request.EventId, request.RefundRequestId, request.PerLineApprovedAmounts?.Count ?? 0);

            try
            {
                var @event = await _eventRepo.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null) return Result.NotFound("Event not found");
                if (!@event.IsOrganizer(request.CallerUserId))
                    return Result.Forbidden("Only organizers of this event can approve refund requests");

                var locator = await _refundRepo.GetByIdAsync(request.RefundRequestId, cancellationToken);
                if (locator is null) return Result.NotFound("Refund request not found");

                var tracked = await _registrationRepo.GetByIdAsync(locator.RegistrationId, cancellationToken);
                if (tracked is null) return Result.Failure("Failed to load registration for update");

                if (tracked.EventId != request.EventId)
                    return Result.NotFound("Refund request does not belong to this event");

                var trackedRequest = tracked.RefundRequests.FirstOrDefault(r => r.Id == request.RefundRequestId);
                if (trackedRequest is null)
                    return Result.NotFound("Refund request not found on registration");

                // Build per-line dictionary. Lines not present default to 0 (Rejected per line).
                var perLine = (request.PerLineApprovedAmounts ?? Array.Empty<ApproveLineItemInputDto>())
                    .ToDictionary(
                        li => li.LineItemId,
                        li => new Money(li.ApprovedAmount, li.Currency));

                // W4.D13.5: invoke via Registration aggregate (not the child entity directly)
                // so the RefundRequestApprovedEvent is re-raised with EventId populated.
                // Calling trackedRequest.Approve(...) directly would only fire the child's
                // event (EventId=Empty) and downstream handlers fail silently → no decision
                // email (the F1/G3 bug fix).
                var approveResult = tracked.ApproveRefundRequest(
                    refundRequestId: request.RefundRequestId,
                    organizerUserId: request.CallerUserId,
                    organizerNotes: request.OrganizerNotes,
                    perLineApprovedAmounts: perLine);

                if (approveResult.IsFailure)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "[6A.148] ApproveRefundRequest Validation FAILED: RrId={RrId} Error={Error}",
                        request.RefundRequestId, approveResult.Error);
                    return Result.Failure(approveResult.Error);
                }

                // Phase 6A.148.D10: pre-commit event audit — surfaces whether the
                // RefundRequestApprovedEvent is queued on the Registration root (which the
                // DomainEventDispatcher pulls from) versus only on the RefundRequest child
                // entity (which the dispatcher might skip). F1 hypothesis G3 in Wave 4 plan.
                _logger.LogInformation(
                    "[6A.148.D10 EVENTS] ApproveRefundRequest pre-commit event audit: RrId={RrId} RegistrationEvents={RegCount} RegistrationEventTypes=[{RegTypes}] RefundRequestEvents={RrCount} RefundRequestEventTypes=[{RrTypes}]",
                    request.RefundRequestId,
                    tracked.DomainEvents.Count,
                    string.Join(",", tracked.DomainEvents.Select(e => e.GetType().Name)),
                    trackedRequest.DomainEvents.Count,
                    string.Join(",", trackedRequest.DomainEvents.Select(e => e.GetType().Name)));

                _registrationRepo.Update(tracked);

                try
                {
                    await _uow.CommitAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    sw.Stop();
                    _logger.LogWarning(ex,
                        "[6A.148] ApproveRefundRequest concurrency conflict: RrId={RrId}; another organizer approved first",
                        request.RefundRequestId);
                    return Result.Conflict(
                        "Another organizer approved or modified this refund request first. Refresh and try again.");
                }

                sw.Stop();
                _logger.LogInformation(
                    "[6A.148] ApproveRefundRequest COMMITTED: RrId={RrId} RegId={RegId} Duration={ElapsedMs}ms; dispatching Stripe (outside transaction)",
                    request.RefundRequestId, tracked.Id, sw.ElapsedMilliseconds);

                // Architect F10: Stripe dispatch runs AFTER commit, in a separate scope.
                // RefundExecutionService is idempotent at the line-item level so retries are safe.
                try
                {
                    var dispatchResult = await _executionService.DispatchAsync(
                        request.RefundRequestId, cancellationToken);
                    if (dispatchResult.IsFailure)
                    {
                        // Approval already committed — Stripe failure is a partial / recoverable
                        // state that RefundReconciliationService picks up (architect F11). Don't
                        // roll back the approval; return success to the organizer with a note.
                        _logger.LogWarning(
                            "[6A.148] Post-commit Stripe dispatch failed for RrId={RrId} Error={Error}; reconciler will retry",
                            request.RefundRequestId, dispatchResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[6A.148] Post-commit Stripe dispatch EXCEPTION for RrId={RrId}; reconciler will retry",
                        request.RefundRequestId);
                    // Swallow — approval is persisted; reconciler retries.
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148] ApproveRefundRequest EXCEPTION: RrId={RrId} Duration={ElapsedMs}ms",
                    request.RefundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
